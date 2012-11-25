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
		public int min_players, max_players;
		public Vector2 bounds;
		public Texture icon;  // TODO
		public List<BaseObject> objects;
		
		public void Spawn() {
			// TODO!
			foreach (var obj in objects) {
				if (obj.prefab != null) {
					obj.Spawn(bounds);
				}
			}
		}
		public void SpawnGrabbables() {
			// TODO!
			foreach (var obj in objects) {
				if (obj.GetType() != typeof(Board)) {  // TODO: More general approach
					obj.Spawn(bounds);
				}
			}
		}
	}
	
	// Base classes for spawnable types
	class BaseObject {
		public GameObject prefab;
		public string name, type;  // Name is not used yet
		public Vector3 pos, size, scale;  // Size & scale overlap, remove one?
		public Quaternion rot;  // Can be given as "horizontal", "vertical_reversed", etc.
		
		virtual public void Spawn(Vector2 bounds) { }
		virtual public void SetRotation(Rotations orientation) {
			// TODO!
		}
	}
	
	class CardsCollection {
		public string type;
		public string src, back;
	}
	
	class CardsCollectionFolder : CardsCollection {
		public string filter;
	}
	
	// Spawnable types
	class Dice : BaseObject {
		public int count;
		override public void Spawn(Vector2 bounds) {
			// Using two-row layout (unlimited columns)
			// TODO: Create better, more square grid for > 4 dice
			var scale = 1.0f;  // Width of one die
			var offset_initial = new Vector3(1.5f * scale, 0, 1.5f * scale);
			var offset_per_column = new Vector3(0, 0, -1.5f * scale);
			var offset_per_row = new Vector3(-1.5f * scale, 0, 0);
			for (int i = 0; i < count; i++) {
				Network.Instantiate(prefab, pos + offset_initial +
					offset_per_row * (i % 2) + offset_per_column * (i / 2), rot, 0);
			}
		}
	}
	
	class Board : BaseObject {
		public string texture, texture_n;
	}
	
	class Deck : BaseObject {
		public List<CardsCollection> cards;
		public bool faceUp;
	}
	
	class Card : BaseObject {
		public string texture;
		public bool faceUp;
	}
}

public class ModManagerScript : MonoBehaviour {
	public GameObject board_prefab;
	public GameObject dice_prefab;
	public GameObject token_prefab;
	public GameObject deck_prefab;		
	public GameObject silver_coin_prefab;	
	public GameObject gold_coin_prefab;		

	ModTypes.Mod active_mod;
	List<ModTypes.Mod> mods;
	List<ModTypes.Card> cards;
	
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
				
				// Required data
				// These exceptions (if any) will be caught by higher try/catch block and halt loading of this mod
				mod.name = (string) dict["game_name"];
				var bounds_temp = (IList) dict["game_bounds"];
				mod.bounds = new Vector2(System.Convert.ToSingle(bounds_temp[0]), System.Convert.ToSingle(bounds_temp[1]));
				
				// Optional data
				if (dict.ContainsKey("game_author"))
					mod.author = (string) dict["game_author"];
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
				
				// Load object list
				mod.objects = new List<ModTypes.BaseObject>();
				var objects = (IList) dict["objects"];
				foreach (var entry in objects) {
					var entry_dict = (Dictionary<string, object>) entry;  // TODO: Catch exception
					var obj_type = (string)entry_dict["type"];
					ModTypes.BaseObject obj_generic = null;
					if (obj_type == "board") {
						var board = new ModTypes.Board();
						if (entry_dict.ContainsKey("texture"))
							board.texture = (string) entry_dict["texture"];
						if (entry_dict.ContainsKey("texture_n"))
							board.texture_n = (string) entry_dict["texture_n"];
						
						obj_generic = board;
						obj_generic.prefab = board_prefab;
						Debug.Log("Obj Board: tex: " + board.texture + ", tex_n: " + board.texture_n);
					} else if (obj_type == "dice") {
						var dice = new ModTypes.Dice();
						if (entry_dict.ContainsKey("count"))
							dice.count = System.Convert.ToInt32(entry_dict["count"]);
						else
							dice.count = 1;
						
						obj_generic = dice;
						obj_generic.prefab = dice_prefab;
						Debug.Log("Obj Dice: count: " + dice.count);
					} else {
						Debug.Log("Obj Unkown: type: " + obj_type);
					}
					
					if (obj_generic != null) {
						// Read generic properties (pos, size, scale, rot)
						if (entry_dict.ContainsKey("pos")) {
							var pos_temp = (IList) entry_dict["pos"];
							// Assume two-tuple for now
							// TODO: Support three-tuple!
							if (pos_temp.Count == 2) {
								obj_generic.pos = new Vector3(System.Convert.ToSingle(pos_temp[0]), System.Convert.ToSingle(pos_temp[1]), 0);
								Debug.Log("pos: " + obj_generic.pos.x + ", " + obj_generic.pos.y);
							} else if (pos_temp.Count == 3) {
								Debug.LogError("Three-component positions not yet supported");
							}
						}
						// TODO: Size / Scale
						// TODO: Rot
						
						mod.objects.Add(obj_generic);
					}
				}
				active_mod = mod;
			}
		}
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
	
	/*
	// Update is called once per frame
	void Update () {
	
	}
	*/
}
