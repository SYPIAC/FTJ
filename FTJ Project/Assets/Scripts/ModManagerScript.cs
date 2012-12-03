using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MiniJSON;
// using JsonFx.Json;

namespace ModTypes {
	enum Rotations {
		horizontal,
		horizontal_reversed,
		vertical,
		vertical_reversed
	};
	
	class Mod {
		public string name, author, path;
		public string base_directory;
		public int min_players, max_players;
		public Vector2 bounds = default_bounds;
		public Texture icon;  // TODO
		public Dictionary<string, BaseObject> assets;
		public List<SpawnRule> instances;
		
		// Approx. size of rendered area in engine units
		private static Vector2 game_area = new Vector2(27.34592f, 16.28584f);
		
		// Default game_bounds in meta.json - scale of game objects (ex. dice) will appear unchanged when these bounds are used
		private static Vector2 default_bounds = new Vector2(170, 100);
		// Default scaling factor if meta.json uses the default game_bounds
		private float default_scale =
			Mathf.Min(game_area.x / default_bounds.x, game_area.y / default_bounds.y);
		
		float GetScalingFactor() {
			if (bounds.x <= 0 || bounds.y <= 0)
				return 1;
			float scale_x = game_area.x / bounds.x;
			float scale_y = game_area.y / bounds.y;
			// Division by default_scale will result in 1 when default game bounds are used
			return Mathf.Min(scale_x, scale_y) / default_scale;
		}
		
		public void Spawn() {
			float scale = GetScalingFactor();
			
			// Adjust table tiling (at default scale it is 4)
			if (scale > 0) {
				GameObject.Find("Table").GetComponent<MeshRenderer>().material.SetTextureScale("_MainTex", new Vector2(1f, 1f) * 4f/scale);
			}
			
			// Spawn objects
			foreach (var sr in instances) {
				sr.Spawn(scale);
			}
		}
		public void SpawnGrabbables() {
			// TODO!!
			/* float scale = GetScalingFactor();
			foreach (var obj in objects) {
				if (obj.GetType() != typeof(Board)) {  // TODO: More general approach
					obj.Spawn(scale);
				}
			} */
		}
	}
	
	class SpawnRule {
		public BaseObject asset;
		public Vector3 pos = Vector3.zero, size = Vector3.one, scale = Vector3.one;  // Combine size / scale??
		public Quaternion rot = Quaternion.identity;
		public string rot_str;  // Rotation can be provided as "horizontal", "vertical reversed", etc.
		public int count = 1;
		
		public void Spawn(float scale) {
			if (rot == null && rot_str != null)
				rot = asset.GetRotation(rot_str);
			
			if (count > 1) {
				int per_row = Mathf.FloorToInt(Mathf.Sqrt(count));
				
				var obj_size = asset.GetSize();
				// Spacing is half object size
				var offset_initial = new Vector3(1.5f * obj_size.x, 0, -1.5f * obj_size.z) *
					(float) per_row / 2;
				var offset_per_column = new Vector3(0, 0, 1.5f * obj_size.z);
				var offset_per_row = new Vector3(-1.5f * obj_size.x, 0, 0);
				
				for (int i = 0; i < count; i++) {
					asset.Spawn(scale, (pos != null ? pos * scale : Vector3.zero) + offset_initial +
						offset_per_row * (i / per_row) + offset_per_column * (i % per_row),
						size, rot);
				}
			} else {
				// Assume count == 1
				// TODO: Format pos test better
				Debug.Log("Spawn type " + asset.GetType().ToString());
				asset.Spawn(scale, pos != null ? pos * scale : Vector3.zero, size, rot);
			}
		}
	}
	
	// Base classes for spawnable types
	class BaseObject {
		public string type;
		
		virtual public void Spawn(float game_scale, Vector3 pos, Vector3 size, Quaternion rot) { }
		virtual public Quaternion GetRotation(string orientation) {
			// Converts a string-rotation representation into a quaternion
			// TODO!
			return Quaternion.identity;
		}
		virtual public Vector3 GetSize() {
			// Used for grid-spawning
			return new Vector3(1, 1, 1);
		}
	}
	
	// Spawnable types
	/* class Dice : BaseObject {
		public int count;

		override public void Spawn(float scale) {
			// Determine number of columns (always have fewer rows than columns)
			var dice_per_row = Mathf.FloorToInt(Mathf.Sqrt(count));
			
			var die_width = 0.4f * scale;  // Dice physics width is 0.4
			var offset_initial = new Vector3(1.5f, 0, -1.5f) * die_width * (int)(dice_per_row / 2);
			var offset_per_column = new Vector3(0, 0, 1.5f * die_width);
			var offset_per_row = new Vector3(-1.5f * die_width, 0, 0);
			
			for (int i = 0; i < count; i++) {
				var obj = Network.Instantiate(prefab, pos * scale + offset_initial +
					offset_per_row * (i / dice_per_row) + offset_per_column * (i % dice_per_row), rot, 0) as GameObject;
				obj.transform.localScale = new Vector3(scale, scale, scale);
			}
		}
	} */
	
	/* class Board : BaseObject {
		public string texture, texture_n;
		
		override public void Spawn(float scale) {
			
		}
	} */
	
	class Deck : BaseObject {
		public string tex_back;
		public List<string> tex_cards;
		public bool face_up = false;
		
		public List<int> card_ids;
		
		override public void Spawn(float game_scale, Vector3 pos, Vector3 size, Quaternion rot) {
			Debug.Log("Spawn deck.");
			// TODO: Refactor location of deck_prefab (should not be in ModManagerScript)
			GameObject deck_object = (GameObject)Network.Instantiate(ModManagerScript.Instance().deck_prefab, pos, rot, 0);
			if (size != Vector3.one) {
				// TODO: Scale
			}
			deck_object.GetComponent<DeckScript>().Fill(card_ids);
		}
		override public Quaternion GetRotation(string orientation) {
			// TODO!
			return Quaternion.identity;
		}
	}
}

public class ModManagerScript : MonoBehaviour {
	// TODO: Default assets
	// d6, other dice
	// tokens
	public GameObject board_prefab;
	public GameObject dice_prefab;
	public GameObject token_prefab;
	public GameObject deck_prefab;
	public GameObject silver_coin_prefab;
	public GameObject gold_coin_prefab;
	
	public Material default_card_front;
	public Material default_card_back;

	ModTypes.Mod active_mod;
	List<ModTypes.Mod> mods;
	
	// Use this for initialization
	void Start () {
		mods = new List<ModTypes.Mod>();

		Debug.Log("Scanning for mods");

		foreach (var dirpath in Directory.GetDirectories("Mods")) {  // TODO: Handle exception
			Debug.Log(dirpath + ": ");
			//try {
			if (true) {
				Debug.Log("attempting to load " + Path.Combine(dirpath, "meta.json"));
				
				var text = File.ReadAllText(Path.Combine(dirpath, "meta.json"));
				Debug.Log(text);
				
				var dict = Json.Deserialize(text) as Dictionary<string, object>;
			
			
				var mod = new ModTypes.Mod();
				
				mod.base_directory = NormalizePath(Path.GetFullPath(dirpath));
				
				// Required data
				// These exceptions (if any) will be caught by higher try/catch block and halt loading of this mod
				mod.name = (string) dict["name"];
				
				// Optional data
				if (dict.ContainsKey("bounds")) {
					var bounds_temp = (IList) dict["bounds"];
					// TODO: Check length, must be two
					mod.bounds = new Vector2(System.Convert.ToSingle(bounds_temp[0]), System.Convert.ToSingle(bounds_temp[1]));
				}
				if (dict.ContainsKey("author"))
					mod.author = (string) dict["author"];
				// TODO: URL??
				// TODO: Icon
				// TODO: Rules path??
				if (dict.ContainsKey("min_players"))
					mod.min_players = System.Convert.ToInt32(dict["min_players"]);
				if (dict.ContainsKey("max_players"))
					mod.max_players = System.Convert.ToInt32(dict["max_players"]);
				
				// Log
				Debug.Log("Mod \"" + mod.name + "\":");
				Debug.Log("Board: " + mod.bounds.x + " x " + mod.bounds.y);
				Debug.Log(mod.min_players + " to " + mod.max_players + " players.");
				
				// Load assets
				mod.assets = new Dictionary<string, ModTypes.BaseObject>();
				var assets = (Dictionary<string, object>) dict["assets"];
				foreach (var asset_pair in assets) {
					var name = asset_pair.Key;
					var asset = (Dictionary<string, object>) asset_pair.Value;
					var type = (string) asset["type"];
					ModTypes.BaseObject generic = null;
					/*if (type == "board") {
						var board = new ModTypes.Board();
						// TODO!
						generic = board;
					} else*/ if (type == "deck") {
						var deck = new ModTypes.Deck();
						
						if (asset.ContainsKey("face_up"))
							deck.face_up = System.Convert.ToBoolean(asset["face_up"]);
						
						deck.tex_back = (string) asset["back"];
						deck.tex_cards = new List<string>();
						var cards = (IList) asset["cards"];
						foreach (var card_obj in cards) {
							var card = (string) card_obj;
							deck.tex_cards.Add(card);
						}
						
						generic = deck;
					}/* else if (type == "token") {
						// TODO!
					}*/ else {
						Debug.Log("Unknown asset type \"" + type + "\", skipping.");
						continue;
					}
					
					if (generic != null)
						mod.assets.Add(name, generic);
					else
						Debug.LogError("New asset is null!");
				}
				
				// TODO: Remove
				Debug.Log("Assets:");
				foreach (var asset_pair in mod.assets) {
					Debug.Log("-  " + asset_pair.Key);
				}
				
				// Read instances
				mod.instances = new List<ModTypes.SpawnRule>();
				var instances = (IList) dict["instances"];
				foreach (var instance_obj in instances) {
					// TODO: Catch next-line exceptions
					var instance = (Dictionary<string, object>) instance_obj;
					
					var sr = new ModTypes.SpawnRule();
					
					var asset_name = (string) instance["asset"];
					var asset = GetAsset(mod.assets, asset_name);  // Gets default assets as well
					if (asset == null) {
						Debug.Log("No such asset: \"" + asset_name + "\"");
						continue;
					}
					sr.asset = asset;
					if (instance.ContainsKey("pos")) {
						var pos_temp = (IList) instance["pos"];
						// Assume two-tuple for now
						// TODO: Support three-tuple!
						if (pos_temp.Count == 2) {
							sr.pos = new Vector3(-System.Convert.ToSingle(pos_temp[0]), 0, -System.Convert.ToSingle(pos_temp[1]));
							Debug.Log("pos: " + sr.pos.x + ", " + sr.pos.y);
						} else if (pos_temp.Count == 3) {
							Debug.LogError("Three-component positions not yet supported");
						}
					}
					// TODO: Size / Scale
					// TODO: Rotation
					// TODO: Count

					mod.instances.Add(sr);
				}
				
				mods.Add(mod);
				SelectMod(mods.Count - 1);
				/*active_mod = mod;
				
				// TODO: Remove this test
				foreach (var asset in mod.assets.Values) {
					if (asset.GetType() == typeof(ModTypes.Deck)) {
						foreach (var tex in ((ModTypes.Deck)asset).tex_cards) {
							GetMaterial(tex);
						}
					}
				}*/
				
				// GetTexture("Raw_Content/_0000_icy_tower.png");
			}
		}
	}
	
	Dictionary<string, ModTypes.BaseObject> defaultAssets = new Dictionary<string, ModTypes.BaseObject>();
	
	void InitializeDefaultAssets() {
		// TODO: d6, tokens
		// TODO: Refactor
	}
	
	ModTypes.BaseObject GetAsset(Dictionary<string, ModTypes.BaseObject> user_defined_assets, string asset_name) {
		// TODO: Refactor, this method shouldn't be here
		if (user_defined_assets.ContainsKey(asset_name))
			return user_defined_assets[asset_name];
		return null;
	}
	
	string NormalizePath(string path) {
		// Ugly fix since Unity does not support paths with backslash separator
		if (Application.platform == RuntimePlatform.WindowsPlayer ||
			Application.platform == RuntimePlatform.WindowsEditor ||
			Application.platform == RuntimePlatform.WindowsWebPlayer)  // This shouldn't matter on web, but...
			path = path.Replace("\\", "/");
		return path;
	}
	
	bool CheckPath(string path) {
		// Checks to see if a path is acceptable - that is, it must be within the active mod dir
		// path must be normalized 
		// TODO: Rename
		// TODO: Test security!
		return path.StartsWith(active_mod.base_directory);
	}
	
	public bool SelectMod(int mod_id) {
		if (mods.Count <= mod_id || mod_id < 0)
			return false;
		
		active_mod = mods[mod_id];
		cardIdCache_ = new Dictionary<KeyValuePair<string, string>, int>();
		cardFronts_ = new Dictionary<int, Material>();
		cardBacks_ = new Dictionary<int, Material>();
		nextCardId = 0;
		textureCache_ = new Dictionary<string, Texture2D>();
		materialCache_ = new Dictionary<string, Material>();
		
		foreach (var asset in active_mod.assets.Values) {
			if (asset.GetType() == typeof(ModTypes.Deck)) {
				var deck = ((ModTypes.Deck)asset);
				deck.card_ids = new List<int>();
				foreach (var tex in deck.tex_cards) {
					// TODO: Use cache
					cardFronts_.Add(nextCardId, GetMaterial(tex));
					cardBacks_.Add(nextCardId, GetMaterial(deck.tex_back));
					
					deck.card_ids.Add(nextCardId);
					
					++nextCardId;
				}
			}
		}
		
		return true;
	}
	
	// private Dictionary<
	private Dictionary<KeyValuePair<string, string>, int> cardIdCache_;
	private Dictionary<int, Material> cardFronts_;
	private Dictionary<int, Material> cardBacks_;
	// private Dictionary<int, string> cardIdCache_;
	private int nextCardId = 0;
	private Dictionary<string, Texture2D> textureCache_ = new Dictionary<string, Texture2D>();
	public Texture2D lastTex;
	
	void PreloadCard(string back, string front) {
		// TODO
		GetMaterial(back);
		GetMaterial(front);
	}
	
	Texture2D GetTexture(string path) {
		if (active_mod == null)
			return null;
		
		path = NormalizePath(Path.Combine(active_mod.base_directory, path));
		
		// Ensure path is valid and belongs to the active mod
		if (!CheckPath(path))
			return null;

		Debug.Log("TexPath: " + path);

		// Load from cache if possible
		if (textureCache_.ContainsKey(path))
			return textureCache_[path];
		
		// Load texture
		// TODO: Threading
		// TODO: Error checking, or just let it use the ? texture
		var www = new WWW("file://" + path);
		lastTex = www.texture;
		Debug.Log(lastTex);
		textureCache_[path] = lastTex;
		return lastTex;
	}
	
	private Dictionary<string, Material> materialCache_ = new Dictionary<string, Material>();
	
	// TODO: Refactor out isFront
	Material GetMaterial(string path) {
		return GetMaterial(path, default_card_front);
	}
	
	Material GetMaterial(string path, Material base_mat) {
		path = NormalizePath(Path.Combine(active_mod.base_directory, path));
		
		// Ensure path is valid and belongs to the active mod
		if (!CheckPath(path))
			return null;
		
		// Check cache
		if (materialCache_.ContainsKey(path))
			return materialCache_[path];
		
		// Load texture
		var tex = GetTexture(path);
		if (tex == null)
			return null;
		
		// Create material
		Material m = new Material(base_mat);
		m.SetTexture("_MainTex", tex);
		
		// Update cache
		materialCache_[path] = m;
		
		return m;
	}
	
	public void SpawnActiveMod() {
		if (networkView.isMine) {  // Necessary?
			active_mod.Spawn();
		}
	}
	
	public void SpawnActiveModGrabbables() {
		if (networkView.isMine) { // Necessary?
			active_mod.SpawnGrabbables();
		}
	}
	
	public static ModManagerScript Instance() {
		if(GameObject.Find("GlobalScriptObject")){
			return GameObject.Find("GlobalScriptObject").GetComponent<ModManagerScript>();
		}
		return null;
	}
	
	public Material GetCardBackMaterial(int id){
		if (cardBacks_.ContainsKey(id))
			return cardBacks_[id];
		return null;
	}
	
	public Material GetCardFrontMaterial(int id){
		if (cardFronts_.ContainsKey(id))
			return cardFronts_[id];
		return null;
	}
}
