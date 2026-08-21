using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace CC
{
    public class CharacterCustomization : MonoBehaviour
    {
        public SkinnedMeshRenderer MainMesh;
        public GameObject UI;
        public string CharacterName;
        public bool Autoload = false;

        public List<scrObj_Hair> HairTables = new List<scrObj_Hair>();
        private List<GameObject> HairObjects = new List<GameObject>();

        public List<scrObj_Apparel> ApparelTables = new List<scrObj_Apparel>();
        private List<GameObject> ApparelObjects = new List<GameObject>();

        public scrObj_Presets Presets;
        public CC_CharacterData StoredCharacterData;

        private string SavePath;

        #region Initialize script

        private void Start()
        {
            SavePath = Application.dataPath + "/CharacterCustomizer.json";

            if (Autoload) Initialize();
        }

        //Initializes this script - run on Start by default but you can run it whenever, see InstantiateCharacter for example
        public void Initialize()
        {
            //Add a blendshape manager script to every mesh
            foreach (var mesh in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                mesh.gameObject.AddComponent<BlendshapeManager>().parseBlendshapes();
            }

            //Adds an empty hair object for each hair table
            for (int i = 0; i < HairTables.Count; i++)
            {
                GameObject newHairObject = new GameObject();
                HairObjects.Add(newHairObject);
                Destroy(newHairObject);
            }

            //Adds an empty apparel object for each apparel table
            for (int i = 0; i < ApparelTables.Count; i++)
            {
                GameObject newApparelObject = new GameObject();
                ApparelObjects.Add(newApparelObject);
                Destroy(newApparelObject);
            }

            //Load character
            LoadFromJSON();

            //Initialize all UI elements (sliders, pickers etc)
            if (UI != null) UI.GetComponent<CC_UI_Util>().Initialize(this);
        }

        #endregion Initialize script

        #region Save & Load

        public void SaveToJSON()
        {
            //Save if file exists, otherwise create a save file and rerun the function
            if (File.Exists(SavePath))
            {
                if (CharacterName != "")
                {
                    //Load CC_SaveData from JSON file
                    string jsonLoad = File.ReadAllText(SavePath);
                    CC_SaveData CC_SaveData = JsonUtility.FromJson<CC_SaveData>(jsonLoad);

                    //Update prefab reference
                    StoredCharacterData.CharacterPrefab = gameObject.name;

                    //Find character index by CharacterName
                    int index = CC_SaveData.SavedCharacters.FindIndex(t => t.CharacterName == CharacterName);

                    //If found, overwrite save data
                    if (index != -1)
                    {
                        CC_SaveData.SavedCharacters[index] = StoredCharacterData;
                    }
                    //Otherwise add new character
                    else
                    {
                        CC_SaveData.SavedCharacters.Add(StoredCharacterData);
                    }

                    //Save to JSON
                    string jsonSave = JsonUtility.ToJson(CC_SaveData, true);
                    File.WriteAllText((SavePath), jsonSave);
                }
            }
            else
            {
                createSaveFile();
                SaveToJSON();
            }
        }

        //Instantiate a character from name, not used anywhere but this is how you would do it
        public void InstantiateCharacter(string name, Transform _transform)
        {
            if (File.Exists(SavePath))
            {
                //Load CC_SaveData from JSON file
                string jsonLoad = File.ReadAllText(SavePath);
                var CC_SaveData = JsonUtility.FromJson<CC_SaveData>(jsonLoad);

                //Find character index by CharacterName and load character data
                int index = CC_SaveData.SavedCharacters.FindIndex(t => t.CharacterName == name);
                if (index != -1)
                {
                    //Instantiate character from resources folder, set name and initialize the script
                    var newCharacter = (GameObject)Instantiate(Resources.Load(CC_SaveData.SavedCharacters[index].CharacterPrefab), _transform);
                    newCharacter.GetComponent<CharacterCustomization>().CharacterName = name;
                    newCharacter.GetComponent<CharacterCustomization>().Initialize();
                }
            }
        }

        public void LoadFromJSON()
        {
            //Load if file exists, otherwise create a save file and rerun the function
            if (File.Exists(SavePath))
            {
                if (CharacterName != "")
                {
                    //Load CC_SaveData from JSON file
                    string jsonLoad = File.ReadAllText(SavePath);
                    CC_SaveData CC_SaveData = JsonUtility.FromJson<CC_SaveData>(jsonLoad);

                    //Find character index by CharacterName and load character data
                    int index = CC_SaveData.SavedCharacters.FindIndex(t => t.CharacterName == CharacterName);
                    if (index != -1)
                    {
                        StoredCharacterData = CC_SaveData.SavedCharacters[index];
                    }
                    //If character is not found, load default character data
                    else
                    {
                        StoredCharacterData = JsonUtility.FromJson<CC_CharacterData>(JsonUtility.ToJson(Presets.Presets[0]));
                        StoredCharacterData.CharacterName = CharacterName;
                    }

                    //Resize lists
                    while (StoredCharacterData.HairNames.Count < HairObjects.Count)
                    {
                        StoredCharacterData.HairNames.Add("");
                    }
                    while (StoredCharacterData.HairColor.Count < HairObjects.Count)
                    {
                        StoredCharacterData.HairColor.Add(new CC_Property());
                    }
                    while (StoredCharacterData.ApparelNames.Count < ApparelObjects.Count)
                    {
                        StoredCharacterData.ApparelNames.Add("");
                    }
                    while (StoredCharacterData.ApparelMaterials.Count < ApparelObjects.Count)
                    {
                        StoredCharacterData.ApparelMaterials.Add(0);
                    }

                    //Apply stored data to character
                    ApplyCharacterVars(StoredCharacterData);
                }
            }
            else
            {
                createSaveFile();
                LoadFromJSON();
            }
        }

        public void ApplyCharacterVars(CC_CharacterData characterData)
        {
            //Set blendshapes
            for (int i = 0; i < characterData.Blendshapes.Count; i++)
            {
                setBlendshapeByName(characterData.Blendshapes[i].propertyName, characterData.Blendshapes[i].floatValue, false);
            }

            //Set hair
            for (int i = 0; i < characterData.HairNames.Count; i++)
            {
                setHairByName(characterData.HairNames[i], i);
            }

            //Set apparel
            for (int i = 0; i < characterData.ApparelNames.Count; i++)
            {
                setApparelByName(characterData.ApparelNames[i], i, characterData.ApparelMaterials[i]);
            }

            //Set texture properties
            foreach (var textureData in characterData.TextureProperties)
            {
                setTextureProperty(textureData, false);
            }

            //Set float properties
            foreach (var floatData in characterData.FloatProperties)
            {
                setFloatProperty(floatData, false);
            }

            //Set color properties
            foreach (var colorData in characterData.ColorProperties)
            {
                Color color;
                if (ColorUtility.TryParseHtmlString("#" + colorData.stringValue, out color))
                {
                    setColorProperty(colorData, color, false);
                };
            }
        }

        public void createSaveFile()
        {
            string json = JsonUtility.ToJson(new CC_SaveData(), true);
            File.WriteAllText(SavePath, json);
        }

        public void setCharacterName(string newName)
        {
            CharacterName = newName;
            StoredCharacterData.CharacterName = newName;
        }

        #endregion Save & Load

        #region Customization

        public void setHair(int selection, int slot)
        {
            if (HairTables[slot].Hairstyles.Count > selection)
            {
                scrObj_Hair.Hairstyle HairData = HairTables[slot].Hairstyles[selection];

                //Destroy active GameObject
                if (HairObjects[slot] != null) GameObject.Destroy(HairObjects[slot]);

                //Set mesh if valid
                if (HairTables[slot].Hairstyles[selection].Mesh != null)
                {
                    HairObjects[slot] = GameObject.Instantiate(HairData.Mesh, gameObject.transform);

                    var HairObject = HairObjects[slot];

                    //Add blendshape managers and update shapes
                    foreach (var mesh in HairObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var manager = mesh.gameObject.AddComponent<BlendshapeManager>();
                        manager.parseBlendshapes();
                        foreach (var shapeData in StoredCharacterData.Blendshapes)
                        {
                            manager.setBlendshape(shapeData.propertyName, shapeData.floatValue);
                        }
                    }

                    //Add CopyPose script
                    if (HairData.AddCopyPoseScript)
                    {
                        HairObject.AddComponent<CopyPose>();
                    }
                    //Otherwise assume hierarchy is the same
                    else
                    {
                        foreach (var mesh in HairObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            var MyBones = new Transform[mesh.bones.Length];
                            for (var i = 0; i < mesh.bones.Length; i++)
                            {
                                if (mesh.bones[i] == null) continue;
                                MyBones[i] = FindChildByName(mesh.bones[i].name, MainMesh.rootBone);
                            }
                            mesh.bones = MyBones;
                        }
                    }
                }

                //Set shadow map
                setTextureProperty(new CC_Property() { propertyName = HairTables[slot].ShadowMapProperty }, false, HairData.ShadowMap);

                //Update hair color
                Color color;
                var colorProperty = StoredCharacterData.HairColor[slot];
                if (ColorUtility.TryParseHtmlString("#" + colorProperty.stringValue, out color))
                {
                    setHairColor(colorProperty, color, slot, false);
                };

                //Update hair name in StoredCharacterData
                StoredCharacterData.HairNames[slot] = HairData.Name;
            }
        }

        public void setHairByName(string name, int slot)
        {
            int index = HairTables[slot].Hairstyles.FindIndex(t => t.Name == name);
            if (index != -1) setHair(index, slot);
        }

        public void setApparel(int selection, int slot, int materialSelection)
        {
            if (ApparelTables[slot].Items.Count > selection)
            {
                scrObj_Apparel.Apparel ApparelData = ApparelTables[slot].Items[selection];

                //Destroy active GameObject
                if (ApparelObjects[slot] != null) GameObject.Destroy(ApparelObjects[slot]);

                //Set mesh if valid
                if (ApparelTables[slot].Items[selection].Mesh != null)
                {
                    ApparelObjects[slot] = GameObject.Instantiate(ApparelData.Mesh, gameObject.transform);

                    var ApparelObject = ApparelObjects[slot];

                    //Add blendshape managers and update shapes
                    foreach (var mesh in ApparelObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var manager = mesh.gameObject.AddComponent<BlendshapeManager>();
                        manager.parseBlendshapes();
                        foreach (var shapeData in StoredCharacterData.Blendshapes)
                        {
                            manager.setBlendshape(shapeData.propertyName, shapeData.floatValue);
                        }
                    }

                    //Set tints
                    foreach (var mesh in ApparelObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        for (var i = 0; i < mesh.materials.Length; i++)
                        {
                            if (ApparelData.Materials.Count == 0 || ApparelData.Materials[0].MaterialDefinitions.Count - 1 < i) continue;
                            var matDefinitions = ApparelData.Materials[materialSelection].MaterialDefinitions;

                            mesh.materials[i].SetColor("_Tint", matDefinitions[i].MainTint);
                            mesh.materials[i].SetColor("_Tint_R", matDefinitions[i].TintR);
                            mesh.materials[i].SetColor("_Tint_G", matDefinitions[i].TintG);
                            mesh.materials[i].SetColor("_Tint_B", matDefinitions[i].TintB);
                            if (matDefinitions[i].Print)
                            {
                                mesh.materials[i].SetTexture("_Print", matDefinitions[i].Print);
                            }
                            else
                            {
                                mesh.materials[i].SetTexture("_Print", Resources.Load<Texture2D>("T_Transparent"));
                            }
                        }
                    }

                    //Add CopyPose script
                    if (ApparelData.AddCopyPoseScript)
                    {
                        ApparelObject.AddComponent<CopyPose>();
                    }
                    //Otherwise assume hierarchy is the same
                    else
                    {
                        foreach (var mesh in ApparelObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            var MyBones = new Transform[mesh.bones.Length];
                            for (var i = 0; i < mesh.bones.Length; i++)
                            {
                                if (mesh.bones[i] == null) continue;
                                MyBones[i] = FindChildByName(mesh.bones[i].name, MainMesh.rootBone);
                            }
                            mesh.bones = MyBones;
                        }
                    }
                }

                //Set foot offset
                if (ApparelData.FootOffset.HeightOffset >= 0)
                {
                    foreach (var component in gameObject.GetComponentsInChildren<TransformBone>())
                    {
                        component.SetOffset(ApparelData.FootOffset);
                    }
                }

                //Set mask
                setTextureProperty(new CC_Property() { propertyName = ApparelTables[slot].MaskProperty }, false, ApparelData.Mask);

                //Update apparel name in StoredCharacterData
                StoredCharacterData.ApparelNames[slot] = ApparelData.Name;
                StoredCharacterData.ApparelMaterials[slot] = materialSelection;
            }
        }

        public void setApparelByName(string name, int slot, int materialSelection)
        {
            int index = ApparelTables[slot].Items.FindIndex(t => t.Name == name);
            if (index != -1) setApparel(index, slot, materialSelection);
        }

        private Transform FindChildByName(string name, Transform parentObj)
        {
            Transform ReturnObj;
            if (parentObj.name == name)
                return parentObj.transform;
            foreach (Transform child in parentObj)
            {
                ReturnObj = FindChildByName(name, child);
                if (ReturnObj)
                    return ReturnObj;
            }
            return null;
        }

        public void setBlendshapeByName(string name, float value, bool save = true)
        {
            if (name != "")
            {
                //Set blendshape on every mesh with a blendshape manager
                {
                    foreach (var manager in gameObject.GetComponentsInChildren<BlendshapeManager>())
                    {
                        manager.setBlendshape(name, value);
                    }
                }

                if (save) saveProperty(ref StoredCharacterData.Blendshapes, new CC_Property() { propertyName = name, floatValue = value });
            }
        }

        public List<Material> getRelevantMaterials(int materialIndex, string meshTag)
        {
            var materials = new List<Material>();

            if (materialIndex != -1)
            //Return materials at specified index
            {
                if (meshTag != "")
                //Return material at index on specified mesh
                {
                    var meshes = getMeshByTag(meshTag);
                    foreach (var mesh in meshes)
                    {
                        if (mesh.materials.Length > materialIndex) materials.Add(mesh.materials[materialIndex]);
                    }
                }
                else
                //Return material at index of all skinned meshes
                {
                    foreach (var mesh in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        if (mesh.materials.Length > materialIndex) materials.Add(mesh.materials[materialIndex]);
                    }
                }
            }
            else
            //Return all materials
            {
                if (meshTag != "")
                //Return all materials on specified mesh
                {
                    var meshes = getMeshByTag(meshTag);
                    foreach (var mesh in meshes)
                    {
                        foreach (var material in mesh.materials)
                        {
                            materials.Add(material);
                        }
                    }
                }
                else
                //Return all materials on all skinned meshes
                {
                    foreach (var mesh in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        foreach (var material in mesh.materials)
                        {
                            materials.Add(material);
                        }
                    }
                }
            }
            return materials;
        }

        public List<SkinnedMeshRenderer> getMeshByTag(string tag)
        {
            var meshes = new List<SkinnedMeshRenderer>();
            foreach (var child in transform.GetComponentsInChildren<Transform>())
            {
                if (child.tag == tag)
                {
                    foreach (var mesh in child.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        meshes.Add(mesh);
                    }
                }
            }
            return meshes;
        }

        //Set texture property
        public void setTextureProperty(CC_Property p, bool save = false, Texture2D t = null)
        {
            //Get relevant materials and set texture
            foreach (var material in getRelevantMaterials(p.materialIndex, p.meshTag))
            {
                material.SetTexture(p.propertyName, (t != null) ? t : (Texture2D)Resources.Load(p.stringValue));
            }

            if (save) saveProperty(ref StoredCharacterData.TextureProperties, p);
        }

        //Set float property
        public void setFloatProperty(CC_Property p, bool save = false)
        {
            //Get relevant materials and set float
            foreach (var material in getRelevantMaterials(p.materialIndex, p.meshTag))
            {
                material.SetFloat(p.propertyName, p.floatValue);
            }

            if (save) saveProperty(ref StoredCharacterData.FloatProperties, p);
        }

        //Set color property
        public void setColorProperty(CC_Property p, Color c, bool save = false)
        {
            //Get relevant materials and set color
            foreach (var material in getRelevantMaterials(p.materialIndex, p.meshTag))
            {
                material.SetColor(p.propertyName, c);
            }

            p.stringValue = ColorUtility.ToHtmlStringRGBA(c);
            if (save) saveProperty(ref StoredCharacterData.ColorProperties, p);
        }

        //Set hair color
        public void setHairColor(CC_Property p, Color color, int slot, bool save = false)
        {
            //Set hair color
            setColorProperty(p, color, false);
            //Set hair tint on head material
            setColorProperty(new CC_Property() { propertyName = HairTables[slot == -1 ? 0 : slot].TintProperty }, color);

            //Save hair color for slot
            if (save)
            {
                if (slot == -1)
                {
                    for (int i = 0; i < StoredCharacterData.HairColor.Count; i++)
                    {
                        StoredCharacterData.HairColor[i] = p;
                    }
                }
                else
                {
                    StoredCharacterData.HairColor[slot] = p;
                }
            }
        }

        //Save property to list, overwrite if already exists
        public void saveProperty(ref List<CC_Property> properties, CC_Property p)
        {
            int savedIndex = properties.FindIndex(t => t.materialIndex == p.materialIndex && t.propertyName == p.propertyName && t.meshTag == p.meshTag);

            if (savedIndex == -1)
            {
                properties.Add(p);
            }
            else
            {
                properties[savedIndex] = p;
            }
        }

        #endregion Customization
    }
}