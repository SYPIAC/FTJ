using UnityEngine;
using System.Collections.Generic;

public class MaterialMaker {
	// int IDs are managed by ModManager
	public int diffuse = -1;
	public int normal = -1;
	// public int mask = -1;

	// String paths will only be available server-side
	public string diffuse_s = null;
	public string normal_s = null;
	// public string mask_s = null;

	public float shininess = -1f;
	public bool transparent = false;
	
	public MaterialMaker() { }
	
	public static MaterialMaker FromJsonObject(Dictionary<string, object> asset, ModManagerScript mod_manager) {
		// TODO: Catch exceptions
		var maker = new MaterialMaker();

		if (asset.ContainsKey("tex"))
			maker.diffuse_s = (string) asset["tex"];
		if (asset.ContainsKey("tex_n"))
			maker.normal_s = (string) asset["tex_n"];
		// TODO: Mask

		if (asset.ContainsKey("tex_shininess")) {
			float shininess = System.Convert.ToSingle(asset["tex_shininess"]);
			if (0f < shininess && shininess <= 1f)
				maker.shininess = shininess;
		}
		if (asset.ContainsKey("tex_transparent")) {
			maker.transparent = System.Convert.ToBoolean(asset["tex_transparent"]);
		}

		return maker;
	}
	
	public void Serialize(BitStream stream) {
		stream.Serialize(ref diffuse);
		stream.Serialize(ref normal);
		stream.Serialize(ref shininess);
		stream.Serialize(ref transparent);
	}
	
	public Material Make(ModManagerScript mod_manager) {
		// TODO: How does transparency and specular interact?

		// Determine shader name
		string shader_name = "Diffuse";
		if (shininess >= 0)
			shader_name = "Specular";
		if (normal != null)
			shader_name = "Bumped " + shader_name;
		if (transparent)
			shader_name = "Transparent/" + shader_name;
		
		// Create material
		var shader = Shader.Find(shader_name);
		Material m = new Material(shader);
		
		// Set material parameters
		if (diffuse != null)
			m.SetTexture("_MainTex", mod_manager.GetTexture(diffuse));
		if (normal != null)
			m.SetTexture("_BumpMap", mod_manager.GetTexture(normal));
		if (shininess >= 0)
			m.SetFloat("_Shininess", shininess);
		
		return m;
	}
	
	public List<string> GetResourceNames() {
		var res = new List<string>();
		if (diffuse_s != null)
			res.Add(diffuse_s);
		if (normal_s != null)
			res.Add(normal_s);
		return res;
	}
	
	public void InitStringIds(ModManagerScript mod_manager) {
		if (diffuse_s != null)
			diffuse = mod_manager.GetStringId(diffuse_s);
		else
			diffuse = -1;
		if (normal_s != null)
			normal = mod_manager.GetStringId(normal_s);
		else
			normal = -1;
		Debug.Log("Diffuse: " + diffuse.ToString());
	}
}
