#pragma once
#include "Scripting/EngineExports.h"

struct ParticleProps_Interop
{
	float Position[2];
	float Velocity[2];
	float VelocityVariation[2];
	float ColorBegin[4];
	float ColorEnd[4];
	float SizeBegin;
	float SizeEnd;
	float SizeVariation;
	float LifeTime;
};

SLIME_EXPORT void* __cdecl ParticleSystem_Create(uint32_t maxParticles);
SLIME_EXPORT void __cdecl ParticleSystem_Destroy(void* system);
SLIME_EXPORT void __cdecl ParticleSystem_OnUpdate(void* system, float ts);
SLIME_EXPORT void __cdecl ParticleSystem_OnRender(void* system);
SLIME_EXPORT void __cdecl ParticleSystem_Emit(void* system, ParticleProps_Interop* props);
