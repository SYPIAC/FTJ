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
	int tex_id_ = -1;
	int tex_n_id_ = -1;
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
	
	public void SetModel(int mesh_id, int tex_id, int tex_n_id) {
		mesh_id_ = mesh_id;
		tex_id_ = tex_id;
		tex_n_id_ = tex_n_id;
		ReloadModel();
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
		
		// TODO: Improve material loading. User should be able to specify the shader type
		var mat = new Material(mod_manager.GetMaterial(tex_id_));
		mat.shader = Shader.Find("Bumped Specular");
		
		var tex_n = mod_manager.GetTexture(tex_n_id_);
		if (tex_n != null)
			mat.SetTexture("_BumpMap", tex_n);
		
		transform.FindChild("default").GetComponent<MeshRenderer>().material = mat;
		// TODO: Texture Normal
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
		if(stream.isWriting) {
			int mesh_id = mesh_id_;
			stream.Serialize(ref mesh_id);
			int tex_id = tex_id_;
			stream.Serialize(ref tex_id);
			int tex_n_id = tex_n_id_;
			stream.Serialize(ref tex_n_id);
			int physics_type = (int) physics_type_;
			stream.Serialize(ref physics_type);
			int physics_mesh_id = physics_mesh_id_;
			stream.Serialize(ref physics_mesh_id);
		} else {
			int mesh_id = -1;
			stream.Serialize(ref mesh_id);
			mesh_id_ = mesh_id;
			int tex_id = -1;
			stream.Serialize(ref tex_id);
			tex_id_ = tex_id;
			int tex_n_id = -1;
			stream.Serialize(ref tex_n_id);
			tex_n_id_ = tex_n_id;
			int physics_type = -1;
			stream.Serialize(ref physics_type);
			physics_type_ = (PhysicsTypes) physics_type;
			int physics_mesh_id = -1;
			stream.Serialize(ref physics_mesh_id);
			physics_mesh_id_ = physics_mesh_id;
			ReloadModel();
			ReloadPhysics();
		}
	}
}
