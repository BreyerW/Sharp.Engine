using System;
using System.Collections.Generic;
using OpenTK;
using Sharp.Editor.Views;
using System.Linq;

namespace Sharp
{
	public class Entity
	{
		/*public Entity ():base()
		{
			position=Vector3.Zero;
			rotation=Vector3.Zero;
			scale=Vector3.One/20;
		}*/
		public Entity parent;
		public HashSet<Entity> childs;
		public string name="Entity Object";
		public Vector3 position=Vector3.Zero;
		public Vector3 rotation=Vector3.Zero;
		public Vector3 scale=Vector3.One;

		public Matrix4 ModelMatrix;
		public Matrix4 MVPMatrix;

		private List<Component> components=new List<Component>();

		public void SetModelMatrix(){
			ModelMatrix=Matrix4.CreateScale(scale)*Matrix4.CreateRotationX(rotation.X) * Matrix4.CreateRotationY(rotation.Y) * Matrix4.CreateRotationZ(rotation.Z) *Matrix4.CreateTranslation(position);
		}

		public T GetComponent<T>() where T : Component{
			return components.OfType<T>().First();
		}
		public Component GetComponent(Type type){
			foreach(var component in components)
				if(component.GetType().GetGenericTypeDefinition()==type)
					return component;
			return null;
		}
		public List<Component> GetAllComponents(){
			return components;
		}
		public T AddComponent<T> () where T : Component, new()
		{
			return AddComponent(new T()) as T;
		}
		public Component AddComponent (Component comp)
		{
			comp.entityObject = this;
			components.Add (comp);
			return comp;
		}

		/*private Behaviour AddComponent (Behaviour comp)
		{
		//assign behaviour specific events to scene view
			components.Add (comp.GetType(),comp);
			return components [comp.GetType()] as Behaviour;
		}
		private Renderer AddComponent (Renderer comp)
		{
		//assign renderer specific events to scene view
			components.Add (comp.GetType(), comp);
			return components [comp.GetType()] as Renderer;
		}*/
		public void Instatiate(){
			SceneView.entities.Add (this);
			SceneStructureView.RegisterEntity (this);
		}
		public void Instatiate(Vector3 pos, Vector3 rot, Vector3 s){
			scale=s;
			position=pos;
			rotation=rot;
			Instatiate();
		}
		public override string ToString ()
		{
			return name;
		}
	}
}

