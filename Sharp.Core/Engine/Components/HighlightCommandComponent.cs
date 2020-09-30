using Sharp.Engine.Components;
using SharpAsset;
using SharpSL.BackendRenderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Core.Engine.Components
{
	class HighlightCommandComponent : CommandBufferComponent
	{
		public HighlightCommandComponent()
		{
			ScreenSpace = true;
			CreateNewTemporaryTexture("renderTarget", TextureRole.Color0, 0, 0, TextureFormat.A);
		}
		public override void Execute()
		{
			throw new NotImplementedException();
		}
	}
}
