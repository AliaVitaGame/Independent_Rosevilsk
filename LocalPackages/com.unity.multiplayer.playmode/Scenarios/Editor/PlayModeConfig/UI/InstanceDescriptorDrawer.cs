#if UNITY_USE_MULTIPLAYER_ROLES
using Unity.Multiplayer.Editor;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.PlayMode.Configurations.Editor;
using Unity.Multiplayer.PlayMode.Editor.Bridge;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Local.Android;
using Unity.Multiplayer.Playmode.Workflow.Editor;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UIElements.Image;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    [CustomPropertyDrawer(typeof(InstanceDescription), true)]
    class InstanceDescriptionDrawer : PropertyDrawer
    {
        internal const string k_InstanceDrawerClass = "instance-drawer";
        const string k_WarnIconClass = "warn-icon";
        const string k_InstanceDisabledHelpboxClass = "instance-disabled-helpbox";
        internal const string k_MultiplayerRolePopupName = "main-multiplayer-role-field";
        internal const string k_MultiplayerAdditionalRolePopupName = "additional-multiplayer-role-field";
        internal const string k_MultiplayerBuildTargetWarnName = "build-target-warn-icon";
        internal const string k_MultiplayerDefaultDeviceName = "Select Device";
        internal const string k_MultiplayerDevicePopupName = "multiplayer-device-popup";
        internal const string k_InstanceDrawerContainerName = "main-multiplayer-instance-drawer-container";
        internal const string k_DisabledInstanceHelpBoxName = "main-multiplayer-instance-DisabledHelpBox";
        internal const string k_DisabledInstanceHelpBoxText = "An instance is currently running. To modify any settings for editor instances, please terminate this instance in the Play Mode Status Window.";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var enumerator = property.GetEnumerator();
            var instanceContainer = new VisualElement() { name = k_InstanceDrawerContainerName };
            var settingsContainer = new VisualElement();
            instanceContainer.Add(settingsContainer);

            var instanceProp = property.Copy();
            settingsContainer.AddToClassList(k_InstanceDrawerClass);

            // When building UI for Free Run Instances, disable modifications if there are actively running ones.
            var scenario = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;
            var instanceDescript = instanceProp.boxedValue as InstanceDescription;
            var hasScenarioAndInstance = scenario != null && instanceDescript != null;
            if (hasScenarioAndInstance && scenario.Scenario != null && !scenario.Scenario.HasActiveFreeRunInstanceOfType(instanceDescript.GetType()))
            {
                settingsContainer.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendAction("Remove", action =>
                    {
                        instanceProp.serializedObject.Update();
                        var so = instanceProp.serializedObject;
                        instanceProp.DeleteCommand();
                        so.ApplyModifiedProperties();
                    });
                    evt.menu.AppendAction("Duplicate", action =>
                    {
                        instanceProp.serializedObject.Update();
                        var so = instanceProp.serializedObject;
                        instanceProp.DuplicateCommand();
                        so.ApplyModifiedProperties();
                    }, _ => ValidateDuplicationOption(_, instanceProp));
                }));
            }

            while (enumerator.MoveNext())
            {
                // CorrespondingNodeID is a property with auto backing field. We don't want to show it in the UI.
                if (property.name == "<CorrespondingNodeId>k__BackingField")
                    continue;

                if (property.name == "Name")
                {
                    var nameField = new TextField();
                    if (property.propertyPath.Contains("Editor"))
                    {
                        var visualElement = nameField.Children().ToList()[0];
                        visualElement.enabledSelf = false;

                    }
                    nameField.AddToClassList("instance-name-field");
                    nameField.label = "Name";
                    nameField.AddToClassList("unity-base-field__aligned");
                    nameField.Bind(property.serializedObject);
                    nameField.BindProperty(property);
                    var nameProp = property.Copy();
                    nameField.RegisterValueChangedCallback(evt =>
                    {
                        var warnIcon = ((VisualElement)evt.target).Q<Image>(className: k_WarnIconClass);
                        ValidateInstanceName(warnIcon, evt.target as TextField);
                    });

                    var warnIcon = CreateBuildProfileWarnIcon("Please select a unique name.");
                    nameField.Insert(1, warnIcon);
                    settingsContainer.Add(nameField);
                    continue;
                }

                if (property.name == "m_BuildProfile")
                {
                    var objectField = new ObjectField() { label = "Build Profile", objectType = typeof(BuildProfile) };
                    objectField.allowSceneObjects = false;
                    objectField.SetValueWithoutNotify(property.objectReferenceValue);
                    objectField.AddToClassList("unity-base-field__aligned");
                    var buildProfileProp = property.Copy();

#if UNITY_USE_MULTIPLAYER_ROLES
                    var roleField = new TextField("Multiplayer Role") { isReadOnly = true, enabledSelf = false, focusable = false };
                    roleField.tooltip = "The multiplayer role first needs to be enabled \n Project Settings > Multiplayer >  Multiplayer Roles > Enable Multiplayer Roles \n then its value can be modified in the build profile window or from the build profile asset.\n ";
                    roleField.AddToClassList("unity-base-field__aligned");
#endif

                    var choices = AndroidUtilities.GetADBDevicesDetailed();
                    var runDeviceField = new PopupField<String>() { label = "Run Device", choices = choices, name = k_MultiplayerDevicePopupName };
                    var buildProfilePropNext = property.Copy();
                    buildProfilePropNext.Next(false);
                    var nextDeviceName = buildProfilePropNext.FindPropertyRelative("m_DeviceName");
                    var nextDeviceID = buildProfilePropNext.FindPropertyRelative("m_DeviceID");
                    var refreshButton = new Button(() => RefreshDeviceList(runDeviceField, buildProfilePropNext))
                    {
                        text = "Refresh"
                    };

                    switch (choices.Count)
                    {
                        case 0:
                            runDeviceField.Children().ToList()[1].SetEnabled(false);
                            runDeviceField.SetValueWithoutNotify("No Devices Available");
                            runDeviceField.tooltip =
                                "No device connected. Please go to https://docs.unity3d.com/Manual/android-debugging-on-an-android-device.html for more information on how to connect a device.";
                            break;
                        case > 0:
                            runDeviceField.SetValueWithoutNotify(k_MultiplayerDefaultDeviceName);
                            runDeviceField.tooltip = "Select the device to run the instance on.";
                            break;
                    }

                    if (nextDeviceName != null)
                    {
                        if (choices.Count == 0 || !choices.Contains(nextDeviceName.stringValue))
                        {
                            nextDeviceID.stringValue = "";
                            nextDeviceID.serializedObject.ApplyModifiedProperties();
                        }
                        else
                        {
                            runDeviceField.SetValueWithoutNotify(nextDeviceName.stringValue == "" ? k_MultiplayerDefaultDeviceName : nextDeviceName.stringValue);
                        }
                    }

                    runDeviceField.AddToClassList("unity-base-field__aligned");
                    runDeviceField.RegisterValueChangedCallback(evt =>
                    {
                        var deviceName = property.FindPropertyRelative("m_DeviceName");
                        deviceName.stringValue = evt.newValue;
                        deviceName.serializedObject.ApplyModifiedProperties();
                        runDeviceField.SetValueWithoutNotify(deviceName.stringValue);
                        var warnIcon = ((VisualElement)evt.target).Q<Image>(className: k_WarnIconClass);
                        SetRunDeviceWarnIcon(warnIcon, runDeviceField);
                        string devices = AndroidUtilities.GetADBDevices();
                        string[] splitDevices = devices.Split("\n");

                        foreach (var device in splitDevices)
                        {
                            string[] deviceParts = device.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                            foreach (var devicePart in deviceParts)
                            {
                                if (evt.newValue.Contains(devicePart))
                                {
                                    var deviceID = devicePart;
                                    var runDeviceID = property.FindPropertyRelative("m_DeviceID");
                                    runDeviceID.stringValue = deviceID;
                                    runDeviceID.serializedObject.ApplyModifiedProperties();
                                }
                            }
                        }
                    });


                    objectField.RegisterValueChangedCallback(evt =>
                    {
                        buildProfileProp.objectReferenceValue = evt.newValue;
                        buildProfileProp.serializedObject.ApplyModifiedProperties();
                        var warnIcon = ((VisualElement)evt.target).Q<Image>(className: k_WarnIconClass);
                        SetBuildProfileWarnIcon(warnIcon, evt.newValue as BuildProfile, instanceProp);

#if UNITY_USE_MULTIPLAYER_ROLES
                        SetRoleDisplay(roleField, buildProfileProp.objectReferenceValue as BuildProfile);
#endif
                        SetDeviceRunDisplay(runDeviceField, buildProfileProp.objectReferenceValue as BuildProfile, instanceProp);
                        SetButtonRefreshDisplay(buildProfileProp.objectReferenceValue as BuildProfile,
                            refreshButton);
                    });

                    var buildProfileButton = new Button(() => EditorApplication.ExecuteMenuItem("File/Build Profiles")) { name = "build-profile-button", text = "Open Build Profiles" };
                    objectField.Add(buildProfileButton);

                    var warnIcon = CreateBuildProfileWarnIcon("Build Profile is not supported, please check File - BuildProfiles for more info.");
                    objectField.Insert(1, warnIcon);
                    SetBuildProfileWarnIcon(warnIcon, buildProfileProp.objectReferenceValue as BuildProfile, instanceProp);

                    settingsContainer.Add(objectField);
#if UNITY_USE_MULTIPLAYER_ROLES
                    settingsContainer.Add(roleField);
                    SetRoleDisplay(roleField, buildProfileProp.objectReferenceValue as BuildProfile);
#endif

                    var deviceWarnIcon = CreateBuildProfileWarnIcon("Please select a device to run the instance on before running the scenario.");
                    runDeviceField.Insert(1, deviceWarnIcon);
                    SetRunDeviceWarnIcon(deviceWarnIcon, runDeviceField);
                    SetDeviceRunDisplay(runDeviceField, buildProfileProp.objectReferenceValue as BuildProfile, instanceProp);
                    SetButtonRefreshDisplay(buildProfileProp.objectReferenceValue as BuildProfile, refreshButton);
                    runDeviceField.Add(refreshButton);
                    settingsContainer.Add(runDeviceField);
                    continue;
                }

                if (property.name.Contains("m_PlayerTag"))
                {
                    List<string> choices = ProjectDataStore.GetMain().GetAllPlayerTags().ToList();
                    choices.Insert(0, "None");
                    var tagsField = new PopupField<String>() { label = "Tag", choices = choices };
                    tagsField.SetValueWithoutNotify(property.stringValue == "" ? "None" : property.stringValue);
                    var tagProp = property.Copy();
                    tagsField.AddToClassList("unity-base-field__aligned");
                    tagsField.tooltip =
                        "Currently only one tag is supported per editor instance. To add a tag to the list of tags go to Project Settings->Multiplayer->Playmode, " +
                        "then select the tag from the dropdown menu.";
                    tagsField.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue == "None")
                        {
                            if (evt.newValue != evt.previousValue)
                            {
                                tagProp.stringValue = "";
                                tagProp.serializedObject.ApplyModifiedProperties();
                            }
                            return;
                        }
                        tagProp.stringValue = evt.newValue;
                        tagProp.serializedObject.ApplyModifiedProperties();
                    });
                    settingsContainer.Add(tagsField);
                    continue;
                }

#if UNITY_USE_MULTIPLAYER_ROLES
                // not needed anymore but keep it because maybe we reenable it.
                if (property.name.Contains("m_Role"))
                {
                    var dropdown = new PopupField<MultiplayerRoleFlags>() { label = "Multiplayer Role" , name = k_MultiplayerAdditionalRolePopupName};

                    dropdown.choices = Enum.GetValues(typeof(MultiplayerRoleFlags)).Cast<MultiplayerRoleFlags>().ToList();
                    MultiplayerRoleFlags currentSelected = (MultiplayerRoleFlags)property.enumValueFlag;
                    dropdown.SetValueWithoutNotify(currentSelected);
                    dropdown.formatSelectedValueCallback = MultiplayerPlayerRoleFlagsText;
                    var enumProp = property.Copy();
                    dropdown.AddToClassList("unity-base-field__aligned");

                    dropdown.RegisterValueChangedCallback(evt =>
                    {
                        if (RoleChoiceIsAllowed(evt.newValue, evt.previousValue, enumProp.serializedObject.targetObject as ScenarioConfig))
                        {
                            enumProp.intValue = (int)evt.newValue;
                            enumProp.serializedObject.ApplyModifiedProperties();

                            return;
                        }

                        dropdown.SetValueWithoutNotify(evt.previousValue);
                    });
                    settingsContainer.Add(dropdown);

                    dropdown.formatListItemCallback = MultiplayerPlayerRoleFlagsText;
                    continue;
                }
#endif

                var propField = new PropertyField(property);
                propField.RegisterValueChangeCallback(evt => { });
                propField.Bind(property.serializedObject);
                settingsContainer.Add(propField);
                if (property.name == "advancedConfiguration" || property.name == "m_AdvancedConfiguration")
                {
                    break;
                }
            }

            DisableInstanceContainerIfFreeRunningActive(settingsContainer, instanceProp);
            return instanceContainer;
        }

#if UNITY_USE_MULTIPLAYER_ROLES
        private string MultiplayerPlayerRoleFlagsText(MultiplayerRoleFlags flag)
        {
            return flag switch
            {
                MultiplayerRoleFlags.ClientAndServer => "Client and Server",
                MultiplayerRoleFlags.Client => "Client",
                MultiplayerRoleFlags.Server => "Server",
                _ => throw new Exception($"Unsupported Multiplayer Player Role Flag: {flag}")
            };
        }
#endif

        private void SetButtonRefreshDisplay(BuildProfile profile, Button button)
        {
            var shouldshow = profile != null && InternalUtilities.IsAndroidBuildTarget(profile);

            button.style.display = shouldshow ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex) : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        static Image CreateBuildProfileWarnIcon(string tooltip)
        {
            var warnIcon = new Image() { name = k_MultiplayerBuildTargetWarnName };
            warnIcon.AddToClassList("propertyfield-icon");
            warnIcon.AddToClassList(k_WarnIconClass);
            warnIcon.image = Icons.GetImage(Icons.ImageName.Warning);
            warnIcon.tooltip = tooltip;
            return warnIcon;
        }

        private static void RefreshDeviceList(PopupField<String> devicerunfield, SerializedProperty property)
        {
            var choices = devicerunfield.choices = AndroidUtilities.GetADBDevicesDetailed();
            var deviceID = property.FindPropertyRelative("m_DeviceID");
            if (choices.Count == 0)
            {
                devicerunfield.value = "No Devices Available";
                devicerunfield.Children().ToList()[2].SetEnabled(false);
                devicerunfield.tooltip =
                    "No device connected. Please go to https://docs.unity3d.com/Manual/android-debugging-on-an-android-device.html for more information on how to connect a device.";
                deviceID.stringValue = "";
            }
            else
            {
                devicerunfield.Children().ToList()[2].SetEnabled(true);
                devicerunfield.value = k_MultiplayerDefaultDeviceName;
                deviceID.stringValue = "";
            }
        }

        DropdownMenuAction.Status ValidateDuplicationOption(DropdownMenuAction arg, SerializedProperty prop)
        {
            var playerInstances = prop.serializedObject.FindProperty("m_EditorInstances");
            if (prop.boxedValue.GetType() == typeof(EditorInstanceDescription) && playerInstances.arraySize >= ScenarioConfigEditor.MaxEditorInstanceCount)
            {
                return DropdownMenuAction.Status.Disabled;
            }

            var localInstances = prop.serializedObject.FindProperty("m_LocalInstances");
            if (prop.boxedValue.GetType() == typeof(LocalInstanceDescription) && localInstances.arraySize >= ScenarioConfigEditor.MaxLocalInstanceCount)
            {
                return DropdownMenuAction.Status.Disabled;
            }

            var remoteInstances = prop.serializedObject.FindProperty("m_RemoteInstances");
            if (prop.boxedValue.GetType() == typeof(RemoteInstanceDescription) && remoteInstances.arraySize >= ScenarioConfigEditor.MaxServerCount)
            {
                return DropdownMenuAction.Status.Disabled;
            }

            return DropdownMenuAction.Status.Normal;
        }

#if UNITY_USE_MULTIPLAYER_ROLES
        void SetRoleDisplay(TextField field, BuildProfile profile)
        {
            field.style.display = profile == null ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            if (profile == null)
                return;

            field.value = EditorMultiplayerRolesManager.GetMultiplayerRoleForBuildProfile(profile).ToString();
        }
#endif

        void SetDeviceRunDisplay(PopupField<string> field, BuildProfile profile, SerializedProperty instance)
        {
            field.style.display = profile == null ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            if (profile == null)
                return;

            var instanceDescript = instance.boxedValue as InstanceDescription;
            var hasValidBuildProfile = IsBuildProfileSupported(profile, instanceDescript, out _);
            field.style.display = hasValidBuildProfile && InternalUtilities.IsAndroidBuildTarget(profile)
                ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        private bool IsBuildProfileSupported(BuildProfile newProfile, InstanceDescription instance, out string error)
        {
            // sanity check
            if (newProfile == null)
            {
                error = "Build Profile is not selected.";
                return false;
            }

            // First check if the specified build target is currently available in the Editor
            if (!InternalUtilities.IsBuildProfileSupported(newProfile))
            {
                error = "Build Profile is not supported or installed for this type of instance.";
                return false;
            }

            // Ensure that it is also supported for the current RuntimePlatform
            var canRun = InternalUtilities.BuildProfileCanRunOnCurrentPlatform(newProfile);
            if (instance is LocalInstanceDescription && !canRun)
            {
                error = "BuildProfile cannot be run as local instance";
                return false;
            }

            // Also ensure instance-type specific build target checks.
            var isServerInstance = instance.GetType() == typeof(RemoteInstanceDescription);
            if (isServerInstance && InternalUtilities.IsAndroidBuildTarget(newProfile))
            {
                error = "Build Profile is not supported or installed for this type of instance.";
                return false;
            }

            error = null;
            return true;
        }

        void SetBuildProfileWarnIcon(Image warnIcon, BuildProfile newProfile, SerializedProperty so)
        {
            if (newProfile == null)
                return;

            var instanceDescript = so.boxedValue as InstanceDescription;
            var isSupported = IsBuildProfileSupported(newProfile, instanceDescript, out string errorMsg);
            if (!isSupported)
                warnIcon.tooltip = errorMsg;

            warnIcon.style.display = isSupported ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }

        private void DisableInstanceContainerIfFreeRunningActive(VisualElement container, SerializedProperty property)
        {
            // Grab the corresponding configs to look for Free Running instances
            var instanceConfig = property.boxedValue as InstanceDescription;
            var scenarioConfig = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;
            if (instanceConfig == null || scenarioConfig == null || scenarioConfig.Scenario == null)
                return;

            // Find the specific instance Type + Name that we are looking for to make the comparison
            if (!scenarioConfig.Scenario.HasActiveFreeRunInstance(name: instanceConfig.Name,
                                                                  type: instanceConfig.GetType()))
                return;

            // Else we've found a player that is active and we should lock configurations
            container.SetEnabled(false);

            // Also show a disabled help box in the container of that instance.
            var disableEditingHelpbox = new HelpBox(k_DisabledInstanceHelpBoxText, HelpBoxMessageType.Info) { name = k_DisabledInstanceHelpBoxName };
            disableEditingHelpbox.AddToClassList(k_InstanceDisabledHelpboxClass);
            container.parent.Add(disableEditingHelpbox);
        }

        void SetRunDeviceWarnIcon(Image warnIcon, PopupField<string> runDeviceField)
        {
            var isSelected = runDeviceField.value != k_MultiplayerDefaultDeviceName && runDeviceField.value != "No Devices Available";


            warnIcon.style.display = isSelected ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }

        void ValidateInstanceName(Image warnIcon, TextField nameField)
        {
            var textfields = nameField.panel.visualTree.Query<TextField>(className: "instance-name-field").ToList();

            List<string> takenNames = new List<string>();
            textfields.ForEach(textField =>
            {
                var warningIcon = textField.Q<Image>();
                if (takenNames.Contains(textField.value))
                {
                    warningIcon.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                }
                else
                {
                    warningIcon.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                    takenNames.Add(textField.value);
                }
            });
        }

#if UNITY_USE_MULTIPLAYER_ROLES
        static bool RoleChoiceIsAllowed(MultiplayerRoleFlags newRole, MultiplayerRoleFlags oldRole, ScenarioConfig config)
        {
            // if it was already a server, it's ok
            if (oldRole != MultiplayerRoleFlags.Client)
                return true;

            if (newRole != MultiplayerRoleFlags.Client)
            {
                if (config == null)
                    return false;

                var currentServerCount = config.EditorInstance.RoleMask != MultiplayerRoleFlags.Client ? 1 : 0;
                currentServerCount += config.VirtualEditorInstances.Count(inst => ScenarioFactory.GetRoleForInstance(inst) != MultiplayerRoleFlags.Client);
                currentServerCount += config.LocalInstances.Count(inst => ScenarioFactory.GetRoleForInstance(inst) != MultiplayerRoleFlags.Client);
                currentServerCount += config.RemoteInstances.Count(inst => ScenarioFactory.GetRoleForInstance(inst) != MultiplayerRoleFlags.Client);

                if (currentServerCount + 1 > ScenarioConfigEditor.MaxServerCount)
                {
                    EditorUtility.DisplayDialog("Info", $"You can only have {ScenarioConfigEditor.MaxServerCount} server instances", "Ok");
                    return false;
                }
            }

            return true;
        }
#endif
    }

    [CustomPropertyDrawer(typeof(RemoteInstanceDescription.AdvancedConfig), true)]
    class AdvancedConfigDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new Foldout();
            container.AddToClassList("unity-base-field__aligned");

            container.text = "Advanced Configuration";
            var instanceProp = property.Copy();
            var enumerator = property.GetEnumerator();

            container.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                var scenario = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;
                var instanceDescript = instanceProp.boxedValue as InstanceDescription;
                var hasScenarioAndInstance = scenario != null && instanceDescript != null;
                if (hasScenarioAndInstance && scenario.Scenario != null && !scenario.Scenario.HasActiveFreeRunInstanceOfType(instanceDescript.GetType()))
                {
                    evt.menu.AppendAction("Remove", action =>
                    {
                        instanceProp.serializedObject.Update();
                        var so = instanceProp.serializedObject;
                        instanceProp.DeleteCommand();
                        so.ApplyModifiedProperties();
                    });
                }
            }));

            while (enumerator.MoveNext())
            {
                if (property.name == "m_Identifier")
                {
                    var nameField = new PropertyField(property);
                    var remoteNameField = new TextField() { label = "Name In Unity Cloud Dashboard", tooltip = RemoteInstanceDescription.k_ServerNameTooltip };

                    nameField.RegisterValueChangeCallback(evt => remoteNameField.value = RemoteInstanceDescription.ComputeMultiplayName(evt.changedProperty.stringValue));
                    nameField.Bind(property.serializedObject);
                    remoteNameField.SetEnabled(false);

                    container.Add(nameField);
                    container.Add(remoteNameField);
                    continue;
                }

                if (property.name == "m_InstanceAmountOfMemoryMB")
                {
                    var slider = UIFactory.CreateStepSlider(property, "Instance Memory", 100, 7500);
                    container.Add(slider);
                    continue;
                }

                if (property.name == "m_InstanceCpuFrequencyMHz")
                {
                    var slider = UIFactory.CreateStepSlider(property, "Instance CPU Clockspeed", 100, 5500);
                    container.Add(slider);
                    continue;
                }

                if (property.name == "m_FleetRegion")
                {
                    // Todo: hook into GetAvailableFleetRegions() when test/multiplay-api lands
                    var choices = new List<String>() { "North America", "Europe", "Asia", "Australia" };
                    var dropDown = new DropdownField("Fleet Region", choices, 0);
                    dropDown.tooltip = property.tooltip;
                    dropDown.BindProperty(property);
                    dropDown.Bind(property.serializedObject);
                    container.Add(dropDown);
                    continue;
                }

                if (property.name == "m_Arguments")
                {
                    var field = UIFactory.CreateTextfieldWithDefault(property, "Arguments");
                    var linkIcon = new Image();
                    linkIcon.AddToClassList("propertyfield-icon");
                    linkIcon.AddToClassList("link-icon");
                    linkIcon.image = Icons.GetImage(Icons.ImageName.Help);
                    linkIcon.RegisterCallback<ClickEvent>(evt =>
                    {
                        Application.OpenURL("https://docs.unity.com/ugs/manual/game-server-hosting/manual/concepts/launch-parameters");
                    });
                    field.Insert(1, linkIcon);
                    container.Add(field);
                    continue;
                }


                var propField = new PropertyField(property);
                propField.RegisterValueChangeCallback(evt => { });
                propField.Bind(property.serializedObject);
                container.Add(propField);

            }

            foreach (var child in container.Children())
            {
                child.AddToClassList("unity-base-field__aligned");
            }

            return container;
        }
    }
}
