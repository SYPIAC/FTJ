using UnityEngine;
using System.Collections;

public class CardScript : MonoBehaviour {	
	int card_id_ = -1;
	int card_back_id_ = -1;
	
	public AudioClip[] impact_sound;
	public AudioClip[] pick_up_sound;
	float last_sound_time = 0.0f;
	const float PHYSICS_SOUND_DELAY = 0.1f;

	[RPC]
	public void PrepareLocal(int card_id, int card_back_id) {
		var card_back = transform.FindChild("Back").transform.FindChild("default");
		card_back.renderer.material = ModManagerScript.Instance().GetMaterial(card_back_id);
		var card_front = transform.FindChild("FrontBorder").transform.FindChild("default");
		card_front.renderer.material = ModManagerScript.Instance().GetMaterial(card_id);
		card_id_ = card_id;
		card_back_id_ = card_back_id;
	}
	
	public void Prepare(int card_id, int card_back_id) {
		if(Network.isServer && networkView){
			networkView.RPC("PrepareLocal", RPCMode.AllBuffered, card_id, card_back_id);
		} else {
			PrepareLocal(card_id, card_back_id);
		}
	}
	
	[RPC]
	public void PickUpSound() {
		if(Network.isServer){
			networkView.RPC("PickUpSound",RPCMode.Others);
		}
		PlayRandomSound(pick_up_sound, 0.1f);
	}
	
	public int card_id(){
		return card_id_;
	}

	public int card_back_id() {
		return card_back_id_;
	}
	
	public CardData card_data() {
		return new CardData(card_id_, card_back_id_);
	}

	public void SetCardID(int id, int back_id){
		card_id_ = id;
		card_back_id_ = back_id;
	}

	public void ClearCard(){
		card_id_ = -1;
		card_back_id_ = -1;
	}
		
	void PlayRandomSound(AudioClip[] clips, float volume){
		audio.PlayOneShot(clips[Random.Range(0,clips.Length)], volume);
	}		
	
	[RPC]
	void ImpactSound(float volume){
		if(Network.isServer){
			networkView.RPC("ImpactSound",RPCMode.Others,volume);
		}
		PlayRandomSound(impact_sound, volume*0.2f);		
	}
	
	void OnCollisionEnter(Collision info){
		if(info.relativeVelocity.magnitude > 1.0f && Time.time > last_sound_time + PHYSICS_SOUND_DELAY) { 
			float volume = info.relativeVelocity.magnitude*0.1f;
			ImpactSound(volume);
			last_sound_time = Time.time;
		}
		if(Network.isServer){
			if(info.collider.GetComponent<DeckScript>()){
				ObjectManagerScript.Instance().NotifyCardHitDeck(gameObject, info.collider.gameObject);
			}
			if(info.collider.GetComponent<CardScript>()){
				ObjectManagerScript.Instance().NotifyCardHitCard(gameObject, info.collider.gameObject);
			}
		}
	}
}
