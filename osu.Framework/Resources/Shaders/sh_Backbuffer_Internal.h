// Automatically included for every vertex shader.
// Apply the following defines to vertex shader files to enable specific optimisations.

// Use the depth definition provided by vertices.
// Must only be used if a depth value is provided by the vertex definition.
// #define BACKBUFFER_USE_VERTEX_DEPTH

attribute float m_BackbufferDrawDepth;
uniform float g_BackbufferDrawDepth;

// Whether the backbuffer is currently being drawn to
uniform bool g_BackbufferDraw;

void main()
{
    {{ real_main }}(); // Invoke real main func

    if (g_BackbufferDraw)
    {
#ifdef BACKBUFFER_USE_VERTEX_DEPTH
        gl_Position.z = m_BackbufferDrawDepth;
#else
        gl_Position.z = g_BackbufferDrawDepth;
#endif
    }
}