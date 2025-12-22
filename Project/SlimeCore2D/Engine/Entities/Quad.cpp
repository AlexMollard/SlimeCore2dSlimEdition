#include "Quad.h"

Quad::Quad()
{
	// Set default values specific to Quads if necessary
	// e.g., this->SetColor(1.0f, 1.0f, 1.0f);
	// But mostly this relies on the base GameObject constructor.
}

Quad::~Quad()
{
	// Resources (like Textures) are usually managed by ResourceManager
	// or owned by the GameObject base, so we don't need to delete them here
	// unless Quad allocated specific heap memory.
}
