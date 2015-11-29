using System;
using System.Collections.Generic;
using OpenTK;
using Sharp.Editor.Views;

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

		private Dictionary<Type, Component> components=new Dictionary<Type, Component>();

		public T GetComponent<T>() where T : Component{
			return components [typeof(T)] as T;
		}
		public T AddComponent<T> () where T : Component, new()
		{
			return AddComponent(new T()) as T;
		}
		public Component AddComponent (Component comp)
		{
			comp.entityObject = this;
			components.Add (comp.GetType(), comp);
			return components [comp.GetType()];
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
			Instatiate (Vector3.Zero, Vector3.Zero, Vector3.One);
		}
		public void Instatiate(Vector3 pos, Vector3 rot, Vector3 s){
			scale=s;
			position=pos;
			rotation=rot;
			SceneView.entities.Add (this);
			SceneStructureView.RegisterEntity (this);
		}
		public override string ToString ()
		{
			return name;
		}
	}
}

