/* 
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class ConfigManager {
    public static string BaseDir = "/sdcard/Android/data/com.curif.Mametemplate2";
    public static string Cabinets = $"{BaseDir}/cabinets";

    public static Color CabinetStandarColor = Color.black;
}

public static class CabinetMaterials {

    public static Material Black; //uses the shade mobile/difusse
    public static Material Red;
    public static Material Yellow;
    public static Material White;
    public static Material CleanWood;
    public static Material TVBorder;
    public static Material Screen;
    public static Material FrontGlassWithBezel;
    public static Material Marquee;
    public static Material CoinSlotPlastic;
    public static Material CoinSlotPlasticDouble;
    // public static Material LeftOrRight; //the material used with stickers in the sides of the cabinet.

    static CabinetMaterials() {
        Material materialBase = Resources.Load<Material>("Cabinets/Materials/CabinetBlack");
        //Cabinet.GetShader("Custom/CabinetShader");
        
        //dynamic
        Black = new Material(materialBase);
        // Black.SetColor("_Color", Color.black);
        // Black.SetFloat("_Metallic", 0.3f);
        // LeftOrRight = new Material(materialBase);
        // LeftOrRight.SetFloat("_Shininess", 0); //if not the sticker will glow.

        Red = new Material(materialBase);
        Red.SetColor("_Color", Color.red);
        
        Yellow = new Material(materialBase);
        Yellow.SetColor("_Color", Color.yellow);

        White = new Material(materialBase);
        White.SetColor("_Color", Color.white);
        
        //pre created in Unity editor
        CleanWood = Resources.Load<Material>("Cabinets/Materials/wood");
        TVBorder = Resources.Load<Material>("Cabinets/Materials/TVBorder");
        Marquee = Resources.Load<Material>("Cabinets/Materials/Marquee");
        Screen = Resources.Load<Material>("Cabinets/Materials/Screen");
        FrontGlassWithBezel = Resources.Load<Material>("Cabinets/Materials/FrontGlass");

        CoinSlotPlastic = Resources.Load<Material>("Cabinets/Materials/CoinSlotPlastic");
        CoinSlotPlasticDouble = Resources.Load<Material>("Cabinets/Materials/CoinSlotPlasticDouble");
    }
}
public class ObjectFactory {
    private static GameObject coinSlotPlastic;
    private static GameObject coinSlotPlasticDouble;
    
    
    static ObjectFactory() {
        coinSlotPlastic = Resources.Load<GameObject>("Cabinets/CoinSlotPlastic");
        coinSlotPlastic.GetComponent<Renderer>().material = CabinetMaterials.CoinSlotPlastic;
        coinSlotPlasticDouble = Resources.Load<GameObject>("Cabinets/CoinSlotPlasticDouble");
        coinSlotPlasticDouble.GetComponent<Renderer>().material = CabinetMaterials.CoinSlotPlasticDouble;
    }

    public static GameObject CoinSlotPlastic(Vector3 position, Quaternion rotation) {
        return GameObject.Instantiate(coinSlotPlastic, position, rotation) as GameObject;
    }
    public static GameObject CoinSlotPlasticDouble(Vector3 position, Quaternion rotation) {
        return GameObject.Instantiate(coinSlotPlasticDouble, position, rotation) as GameObject;
    }

}

public class Cabinet {
    public string Name = "";
    public GameObject gameObject;
    // private GameObject Center, Front, Joystick, Left, Marquee, Right, Screen;
    // private Material sharedMaterial;
    Dictionary<string, GameObject> Parts = new();

    //Al the parts in the gabinet that must exists, can be others.
    static List<string> RequiredParts = new List<string>() {"Front", "Left", "Right", "Joystick", "Screen", "ScreenBorder", "ScreenBase", "FrontGlass"};
    //Known parts that can exists or not.
    static List<string> OptionalParts = new List<string>() {"Marquee", "FrontGlass", "CoinSlotMarker"};
    //those parts that are part of the structure, like wood parts, all can have the same material. Is a subset of ExpectedParts
    static List<string> CommonParts = new List<string>() {"Center", "Front", "Left", "Right", "Joystick", "ScreenBase"};
    
    public static Shader GetShader(string name) {
        Shader shader = Shader.Find(name);
        if (shader == null || shader.ToString() == "Hidden/InternalErrorShader (UnityEngine.Shader)") {
            UnityEngine.Debug.LogError($"Internal error, Shader not found: {name}");
            shader = Shader.Find("Standard");
        }
        return shader;
    }
    
    // load a texture from disk.
    private static Texture2D LoadTexture(string filePath) {
 
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))     {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        return tex;
    }
    public bool IsValid {
        get {
            foreach (string key in RequiredParts) {
                if (! Parts.ContainsKey(key)) {
                    return false;
                }
            }
            return true;
        }
    } 

    public Cabinet(string name, Vector3 position, Quaternion rotation, GameObject go = null,  string model = "cabinet") {
        Name = name;
        if (go == null) {
            // Assets/Resources/Cabinets/xxx.prefab
            go = Resources.Load($"Cabinets/{model}") as GameObject;
            if (go == null) {
                throw new System.Exception($"Cabinet {Name} not found or doesn't load");
            }
        }
        
        //https://docs.unity3d.com/ScriptReference/Object.Instantiate.html
        gameObject = GameObject.Instantiate(go, position, rotation) as GameObject;

        // https://stackoverflow.com/questions/40752083/how-to-find-child-of-a-gameobject-or-the-script-attached-to-child-gameobject-via
        for (int i = 0; i < gameObject.transform.childCount; i++) {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            if (child != null) {
                Parts.Add(child.name, child);
                Debug.Log(child.name);
            }
        }
        if (!IsValid) {
            throw new System.Exception($"Malformed Cabinet, some parts are missing. List of expected parts: {RequiredParts.ToString()}");
        }

        BoxCollider bc = gameObject.AddComponent(typeof(BoxCollider)) as BoxCollider;
        
        SetMaterial(CabinetMaterials.Black);
        if (Parts.ContainsKey("FrontGlass")) {
            SetMaterial("FrontGlass", CabinetMaterials.FrontGlassWithBezel);
        }
        SetMaterial("Screen", CabinetMaterials.TVBorder);
        SetMaterial("ScreenBorder", CabinetMaterials.TVBorder);

        /*
        var tFront = gameObject.transform.Find("Front");
        var tJoystick = gameObject.transform.Find("Joystick");
        var tCenter = gameObject.transform.Find("Center");
        var tLeft = gameObject.transform.Find("Left");
        var tMarquee = gameObject.transform.Find("Marquee");
        var tRight = gameObject.transform.Find("Right");
        var tScreen = gameObject.transform.Find("Screen");
        var tGlass = gameObject.transform.Find("Glass");

        if (tFront == null || tJoystick == null || tCenter == null || tLeft == null || tRight == null || tScreen == null) {
            throw new System.Exception("Malformed Cabinet, some meshes are missing.");
        }
        parts.Add("Center", tCenter.gameObject);
        parts.Add("Front", tFront.gameObject);
        parts.Add("Joystick", tJoystick.gameObject);
        parts.Add("Left", tLeft.gameObject);
        parts.Add("Right", tRight.gameObject);
        parts.Add("Screen", tScreen.gameObject);
        if (tMarquee != null) {
            parts.Add("Marquee", tMarquee.gameObject);
        }
        if (tGlass != null) {
            parts.Add("Glass", tGlass.gameObject);
        }
        */
    }
    public GameObject this[string part] {
        get {
            return Parts[part];
        }
    }

    

    public Cabinet SetTextureTo(string cabinetPart, string textureFile, Material mat, bool invertX = false, bool invertY = false) {

        if (! Parts.ContainsKey(cabinetPart)) {
            // throw new System.Exception($"Unrecognized part: {cabinetPart} adding the texture: {textureFile}");
            return this;
        }
       
        //main texture
        Texture2D t = LoadTexture($"{ConfigManager.Cabinets}/{textureFile}");
        if (t == null) {
            Debug.Log($"Cabinet {Name} marquee texture {textureFile} not found in disk");
        }
        else {
            
            Material m = new Material(mat);
            m.SetTexture("_MainTex", t);
            
            //tiling
            Vector2 mainTextureScale = new Vector2(1, 1);
            if (invertX) {
                mainTextureScale.x = -1;
            }
            if (invertY) {
                mainTextureScale.y = -1;
            }
            m.mainTextureScale = mainTextureScale;
            
            Parts[cabinetPart].GetComponent<Renderer>().material = m;
        }
        return this;
    }

    public Cabinet SetMarquee(string marqueeTextureFile) {
        if (!Parts.ContainsKey("Marquee")) {
            return this;
        }

        Material m = new Material(CabinetMaterials.Marquee);

        //main texture
        Texture2D t = LoadTexture($"{ConfigManager.Cabinets}/{marqueeTextureFile}");
        if (t == null) {
            Debug.Log($"Cabinet {Name} marquee texture not found in disk");
        }
        else {
            m.SetTexture("_MainTex", t);
        }

        Parts["Marquee"].GetComponent<Renderer>().material = m;
        return this;
    }

    //set the same material to all components.
    public Cabinet SetMaterial(Material mat) {
        foreach(KeyValuePair<string, GameObject> part in Parts) {
            part.Value.GetComponent<Renderer>().material = mat;
        }
        return this;
    }

    //set the material to a component.
    public Cabinet SetMaterial(string part, Material mat) {
        if (! Parts.ContainsKey(part)) {
            throw new System.Exception($"Unknown part {part} to set material in cabinet {Name}");
        }
        Parts[part].GetComponent<Renderer>().material = mat;
        return this;
    }

    public Cabinet SetScreen(string GameFile, bool invertX = false, bool invertY = false) {

        Parts["ScreenBorder"].GetComponent<Renderer>().material = CabinetMaterials.TVBorder;
        
        Material m = new Material(CabinetMaterials.Screen);
        // //tiling
        // Vector2 mainTextureScale = new Vector2(1, 1);
        // if (invertX) {
        //     mainTextureScale.x = -1;
        // }
        // if (invertY) {
        //     mainTextureScale.y = -1;
        // }
        // m.mainTextureScale = mainTextureScale;
        if (invertX) {
            m.SetFloat("MIRROR_X", 1f);
        }
        if (invertY) {
            m.SetFloat("MIRROR_Y", 1f);
        }

        Parts["Screen"].GetComponent<Renderer>().material = m;

        MeshRenderer mr = Parts["Screen"].GetComponent<MeshRenderer>();
        mr.receiveShadows = false;
 
        LibretroScreenController libretroScreenController = Parts["Screen"].AddComponent(typeof(LibretroScreenController)) as LibretroScreenController;
        // Parts["Screen"].AddComponent(typeof(MeshRenderer)); added by default (as sound)
        Parts["Screen"].AddComponent(typeof(MeshCollider));
        // AudioSource audioSource = Parts["Screen"].AddComponent(typeof(AudioSource)) as AudioSource;

        libretroScreenController.GameFile = GameFile;

        return this;
    }

    public Cabinet AddCoinSlot(string type) {
        if (! Parts.ContainsKey("CoinSlotMarker")) {
            return this;
        }

        Vector3 pos = Parts["CoinSlotMarker"].transform.position;
        Quaternion rot = Parts["CoinSlotMarker"].transform.rotation;
        
        if (type == "plastic") {
            GameObject go = ObjectFactory.CoinSlotPlastic(pos, rot);
            go.transform.parent = gameObject.transform;
            Parts.Add("CoinSlot",go);
        }
        else if (type == "plasticDouble") {
            GameObject go = ObjectFactory.CoinSlotPlasticDouble(pos, rot);
            go.transform.parent = gameObject.transform;
            Parts.Add("CoinSlot",go);
        }

        Object.Destroy(Parts["CoinSlotMarker"]);
        return this;
    }

    public Cabinet SetBezel(string bezelPath, bool invertX = false, bool invertY = false) {
        SetTextureTo("FrontGlass", bezelPath, CabinetMaterials.FrontGlassWithBezel, invertX: invertX, invertY: invertY);
        return this;
    }

}

//store Cabinets resources
public static class CabinetFactory {
    static Dictionary<string, GameObject> CabinetStyles = new();

    static CabinetFactory() {
        CabinetStyles.Add("Generic",  Resources.Load($"Cabinets/Generic") as GameObject);
        CabinetStyles.Add("TimePilot",  Resources.Load($"Cabinets/TimePilot") as GameObject);
    }

    public static Cabinet Factory(string style, string name, Vector3 position, Quaternion rotation) {
        if (!CabinetStyles.ContainsKey(style)) {
            throw new System.Exception("Cabinet not found in Factory");
        }

        return new Cabinet(name, position, rotation, CabinetStyles[style]);
    }
}

public class Init {
    //https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute-ctor.html
    [RuntimeInitializeOnLoadMethod]
    static void OnRuntimeMethodLoad()
    {
        Debug.Log("+++++++++++++++++++++  Initialize Cabinets +++++++++++++++++++++");
        
        if (!Directory.Exists(ConfigManager.Cabinets)) {
            // Directory.CreateDirectory(BaseDir);
            Directory.CreateDirectory(ConfigManager.Cabinets);
        }

        // position.z = -7f;
        GameObject CabSpot = GameObject.Find("CabSpot");
        Cabinet c2 = CabinetFactory.Factory("TimePilot", "timeplt", CabSpot.transform.position, CabSpot.transform.rotation);
        c2.SetMaterial(CabinetMaterials.Black)
            .SetMarquee("ast-deluxe-marquee_orig.jpg")
            .SetTextureTo("Left", "side_left-rec.png", CabinetMaterials.Black, invertX: true)
            .SetTextureTo("Right", "side_left-rec.png", CabinetMaterials.Black)
            .SetMaterial("ScreenBase", CabinetMaterials.CleanWood)
            .SetBezel("bezeltest.png")
            .AddCoinSlot("plasticDouble")
            .SetScreen("timeplt.zip", invertY: true, invertX: false);
        Object.Destroy(CabSpot);

        CabSpot = GameObject.Find("CabSpot2");
        c2 = CabinetFactory.Factory("Generic", "generic-black", CabSpot.transform.position, CabSpot.transform.rotation);
        c2.SetMaterial(CabinetMaterials.Black)
            .SetMarquee("ast-deluxe-marquee_orig.jpg")
            .SetTextureTo("Left", "side_left-rec.png", CabinetMaterials.Black, invertX: true)
            .SetTextureTo("Right", "side_left-rec.png", CabinetMaterials.Black)
            .SetMaterial("ScreenBase", CabinetMaterials.CleanWood)
            .AddCoinSlot("plastic")
            .SetBezel("bezeltest.png")
            .SetScreen("arkanoid.zip", invertY: true, invertX: false);
        Object.Destroy(CabSpot);

        GameObject[] emptyCabs;
        emptyCabs = GameObject.FindGameObjectsWithTag("Cabinet");
        foreach (GameObject cab in emptyCabs) {
            //just one for now
            Vector3 pos = cab.transform.position;
            Quaternion rot = cab.transform.rotation;
            Cabinet c = CabinetFactory.Factory("Generic", "respawned", pos, rot);
            c.SetTextureTo("Left", "side_left-rec.png", CabinetMaterials.Black)
            .SetTextureTo("Right", "side_left-rec.png", CabinetMaterials.Black, invertX: true)
            .SetMaterial("ScreenBase", CabinetMaterials.CleanWood)
            .AddCoinSlot("plastic")
            .SetBezel("bezeltest.png")
            .SetScreen("galaga.zip", invertY: true, invertX: false);

            Object.Destroy(cab);
            break;
        }
                
        Debug.Log("+++++++++++++++++++++ Initialized");

    }
}
