using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class AIBehaviour : NetworkBehaviour
    {
        public string navmeshAgentName = "Humanoid";

        private float movementSpeed;
        private float smoothMovementTime = 0.1f;

        private WeaponControlSystem weaponControlSystem;
        private HealthController healthController;
        private ModifiersControlSystem modifiersControlSystem;
        private RigidbodyCharacterController rigidbodyCharacterController;
        private CharacterIdentityControl identityControl;

        private float distanceToOpenFire;
        private float targetUpdateRate;
        private float maneuverAngle;
        private float minDistanceOfManeuver;
        private float maxDistanceOfManeuver;
        private float minDistanceToUpdateManeuverPoint;
        private float maneuverExitTime;

        private Vector3 movementVelocity = Vector3.zero;
        private Vector3 currentMovementInput;

        private Transform targetTransform;
        private Vector3 moveToPoint;
        private float lastPointUpdateTime = 0;


        private void Start()
        {
            //Basic components
            weaponControlSystem = GetComponent<WeaponControlSystem>();
            healthController = GetComponent<HealthController>();
            modifiersControlSystem = GetComponent<ModifiersControlSystem>();
            rigidbodyCharacterController = GetComponent<RigidbodyCharacterController>();
            identityControl = GetComponent<CharacterIdentityControl>();

            //Settings
            int modelIndex = identityControl.spawnParameters.Value.modelIndex;
            AIConfig config = SettingsManager.Instance.ai.configs[modelIndex];

            //Health
            healthController.Initialize(config.health);
            healthController.OnDeath += () => GetComponent<CharacterEffectsController>().RunDestroyScenario(false);

            //Register spawned bot
            GameManager.Instance.userControl.AddAIObject(NetworkObject);

            movementSpeed = config.movementSpeed;

            //Target & fire
            distanceToOpenFire = config.distanceToOpenFire;
            targetUpdateRate = config.targetUpdateRate;

            //Maneuver
            maneuverAngle = config.maneuverAngle;
            minDistanceOfManeuver = config.minDistanceOfManeuver;
            maxDistanceOfManeuver = config.maxDistanceOfManeuver;
            minDistanceToUpdateManeuverPoint = config.minDistanceToUpdateManeuverPoint;
            maneuverExitTime = config.maneuverExitTime;

            gameObject.name = config.botPrefab.name;

            StartCoroutine(RunUpdateTargetLoop());
        }

        private void FixedUpdate()
        {
            if (IsOwner == false) return;

            //Stop any movement when game ends
            if (GameManager.Instance.gameState == GameState.GameIsOver) rigidbodyCharacterController.Stop();

            //Stop any movement when bot is dead
            if (healthController.isAlive == false) rigidbodyCharacterController.Stop();

            //Wait for game to start
            if (GameManager.Instance.gameState != GameState.ActiveGame) return;

            if (targetTransform != null)
            {
                Vector3 lookDirection = targetTransform.position - transform.position;
                lookDirection.y = 0;

                weaponControlSystem.lineOfSightTransform.localRotation = Quaternion.LookRotation(lookDirection);

                Vector3 moveDirection = moveToPoint - transform.position;

                //Update maneuver point if bot is close enough or can't reach moveToPoint before exit time (probably it's stuck)
                if (moveDirection.magnitude < minDistanceToUpdateManeuverPoint || Time.time > lastPointUpdateTime + maneuverExitTime)
                {
                    Vector3 maneuverPoint = CalculateManeuverPoint();
                    moveToPoint = GetNextNavigationPoint(maneuverPoint);
                    moveDirection = moveToPoint - transform.position;
                    lastPointUpdateTime = Time.time;
                }

                //Update movement speed according to currently active modifiers
                currentMovementInput = Vector3.SmoothDamp(currentMovementInput, moveDirection.normalized, ref movementVelocity, smoothMovementTime);
                float currentSpeed = modifiersControlSystem.CalculateSpeedMultiplier() * movementSpeed;

                //Move (using physics)
                rigidbodyCharacterController.Move(currentMovementInput, currentSpeed);

                //Fire weapon
                if (lookDirection.magnitude < distanceToOpenFire)
                    weaponControlSystem.Fire();
            }
        }

        private Vector3 CalculateManeuverPoint()
        {
            float halfAngle = maneuverAngle * 0.5f;
            Quaternion lookRotation = Quaternion.LookRotation(transform.position - targetTransform.position);
            lookRotation *= Quaternion.Euler(0, Random.Range(-halfAngle, halfAngle), 0);

            //Get random point around target
            return targetTransform.position + (lookRotation * Vector3.forward) * Random.Range(minDistanceOfManeuver, maxDistanceOfManeuver);
        }

        private IEnumerator RunUpdateTargetLoop()
        {
            while (healthController.isAlive)
            {
                targetTransform = FindNearestTarget();
                yield return new WaitForSeconds(targetUpdateRate);
            }
        }

        private Transform FindNearestTarget()
        {
            //List of targets (could be any other collection)
            List<NetworkObject> targets = GameManager.Instance.userControl.allCharacters;

            Transform nearestTarget = null;
            float minDistance = float.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                //Skip ourselves and null elements (just in case)
                if (targets[i] == null || targets[i].transform == transform) continue;

                //Distance to next character
                float distance = (transform.position - targets[i].transform.position).magnitude;

                //Find closest
                if (distance < minDistance)
                {
                    nearestTarget = targets[i].transform;
                    minDistance = distance;
                }
            }

            return nearestTarget;
        }

        //NavMesh path calculation
        protected Vector3 GetNextNavigationPoint(Vector3 targetPoint)
        {
            NavMeshPath path = new NavMeshPath();

            NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter();
            navMeshQueryFilter.areaMask = NavMesh.AllAreas;

            //Calculate path for specific navmesh agent
            navMeshQueryFilter.agentTypeID = GetAgentIdByName(navmeshAgentName);

            float maxDistanceFromNavMesh = 1;

            //Get closest point on navmesh
            if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, maxDistanceFromNavMesh, navMeshQueryFilter))
            {
                //Calculate path
                NavMesh.CalculatePath(transform.position, hit.position, navMeshQueryFilter, path);
            }
            else
                Debug.Log("Unable to calculate path. Target point is too far from NavMesh.");

            if (Application.isEditor)
                DrawPath(path.corners, 1, Color.red);

            if (path.corners.Length > 1)
                return path.corners[1];
            else
                //Return character position if path is too short
                return targetPoint;
        }

        private void DrawPath(Vector3[] corners, float duration, Color color)
        {
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Debug.DrawLine(corners[i], corners[i + 1], color, duration);
            }
        }

        public int GetAgentIdByName(string agentName)
        {
            var count = NavMesh.GetSettingsCount();
            var agentTypeNames = new string[count];

            for (var i = 0; i < count; i++)
            {
                var id = NavMesh.GetSettingsByIndex(i).agentTypeID;
                var name = NavMesh.GetSettingsNameFromID(id);
                agentTypeNames[i] = name;

                if (name == agentName)
                    return id;
            }

            Debug.Log("Agent name: " + agentName + " does not exists.");
            return 0;
        }
    }
}
