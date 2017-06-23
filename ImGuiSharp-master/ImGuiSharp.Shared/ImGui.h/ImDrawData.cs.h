﻿// All draw data to render an ImGui frame
struct ImDrawData
{
	bool            Valid;                  // Only valid after Render() is called and before the next NewFrame() is called.
	ImDrawList**    CmdLists;
	int             CmdListsCount;
	int             TotalVtxCount;          // For convenience, sum of all cmd_lists vtx_buffer.Size
	int             TotalIdxCount;          // For convenience, sum of all cmd_lists idx_buffer.Size

											// Functions
	ImDrawData() { Valid = false; CmdLists = NULL; CmdListsCount = TotalVtxCount = TotalIdxCount = 0; }
	IMGUI_API void DeIndexAllBuffers();               // For backward compatibility: convert all buffers from indexed to de-indexed, in case you cannot render indexed. Note: this is slow and most likely a waste of resources. Always prefer indexed rendering!
	IMGUI_API void ScaleClipRects(const ImVec2& sc);  // Helper to scale the ClipRect field of each ImDrawCmd. Use if your final output buffer is at a different scale than ImGui expects, or if there is a difference between your window resolution and framebuffer resolution.
};