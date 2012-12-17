using UnityEngine;
using System.Collections;

public class DiceScript : MonoBehaviour {
	public AudioClip[] dice_impact_wood;
	public AudioClip[] dice_impact_board;
	public AudioClip[] dice_impact_dice;
	public AudioClip[] dice_pick_up;
	float last_sound_time = 0.0f;
	const float PHYSICS_SOUND_DELAY = 0.1f;
	const float DICE_GLOBAL_SOUND_MULT = 0.3f;
	const float DICE_BOARD_SOUND_MULT = 0.4f;
	const float DICE_WOOD_SOUND_MULT = 1.0f;
	const float DICE_DICE_SOUND_MULT = 1.0f;
	
	int mesh_id_ = -1;
	MaterialMaker material_maker_ = null;
	enum PhysicsTypes {
		box,
		model
	};
	PhysicsTypes physics_type_ = PhysicsTypes.model;
	public int physics_mesh_id_ = -1;
	
	void PlayRandomSound(AudioClip[] clips, float volume){
		audio.PlayOneShot(clips[Random.Range(0,clips.Length)], volume);
	}		
	
	[RPC]
	public void ShakeSound(){
		if(Network.isServer){
			networkView.RPC("ShakeSound",RPCMode.Others);
		}
		PlayRandomSound(dice_impact_dice, DICE_DICE_SOUND_MULT*0.05f);
	}
	
	[RPC]
	public void PickUpSound() {
		if(Network.isServer){
			networkView.RPC("PickUpSound",RPCMode.Others);
		}
		PlayRandomSound(dice_pick_up, 0.1f);
	}
	
	[RPC]
	public void PlayImpactSound(int layer, float volume) {
		if(Network.isServer){
			networkView.RPC("PlayImpactSound",RPCMode.Others,layer,volume);
		}
		int table_layer = LayerMask.NameToLayer("Table");
		int board_layer = LayerMask.NameToLayer("Board");
		int card_layer = LayerMask.NameToLayer("Cards");
		if(layer == table_layer){
			PlayRandomSound(dice_impact_wood, volume*DICE_WOOD_SOUND_MULT*DICE_GLOBAL_SOUND_MULT);
		} else if(layer == board_layer || layer == card_layer){			
			PlayRandomSound(dice_impact_board, volume*DICE_BOARD_SOUND_MULT*DICE_GLOBAL_SOUND_MULT);
		} else {
			PlayRandomSound(dice_impact_dice, volume*DICE_DICE_SOUND_MULT*DICE_GLOBAL_SOUND_MULT);
		}			
	}
	
	void OnCollisionEnter(Collision info) {
		if(info.relativeVelocity.magnitude > 1.0f && Time.time > last_sound_time + PHYSICS_SOUND_DELAY) { 
			float volume = info.relativeVelocity.magnitude*0.1f;
			int layer = info.collider.gameObject.layer;
			PlayImpactSound(layer,volume);
			last_sound_time = Time.time;
		}	
	}
	
	public void SetModel(int mesh_id) {
		mesh_id_ = mesh_id;
		ReloadModel();
	}
	
	public void SetMaterial(MaterialMaker material_maker) {
		material_maker_ = material_maker;
		ReloadMaterial();
	}
	
	public void SetPhysicsModel(int physics_mesh_id) {
		physics_mesh_id_ = physics_mesh_id;
		ReloadPhysics();
	}
	
	public void SetPhysicsBox() {
		physics_type_ = PhysicsTypes.box;
		// TODO: Size
		ReloadPhysics();
	}
	
	void ReloadModel() {
		ModManagerScript mod_manager = ModManagerScript.Instance();
		transform.FindChild("default").GetComponent<MeshFilter>().mesh = mod_manager.GetMesh(mesh_id_);
	}
	
	void ReloadMaterial() {
		ModManagerScript mod_manager = ModManagerScript.Instance();
		
		var mat = material_maker_.Make(mod_manager);
		transform.FindChild("default").GetComponent<MeshRenderer>().material = mat;
	}
	
	void ReloadPhysics() {
		ModManagerScript mod_manager = ModManagerScript.Instance();
		
		if (true) { // physics_type_ == PhysicsTypes.model) {  // TODO: check physics type
			var mesh = mod_manager.GetMesh(physics_mesh_id_);
			if (mesh != null) {
				MeshCollider mc = gameObject.GetComponent<MeshCollider>();
				if (mc == null) {
					// Destroy any existing collider
					if (collider != null)
						Destroy(collider);
					mc = gameObject.AddComponent<MeshCollider>();
				}
				mc.sharedMesh = mesh;
				mc.convex = true;
			}
		} else if (physics_type_ == PhysicsTypes.box) {
			// TODO: Physics box
		}
	}
	
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) {
		stream.Serialize(ref mesh_id_);

		if (material_maker_ == null)
			material_maker_ = new MaterialMaker();
		material_maker_.Serialize(stream);

		int physics_type = (int) physics_type_;
		stream.Serialize(ref physics_type);
		physics_type_ = (PhysicsTypes) physics_type;

		stream.Serialize(ref physics_mesh_id_);

		if (!stream.isWriting) {
			ReloadModel();
			ReloadMaterial();
			ReloadPhysics();
		}
	}
}
