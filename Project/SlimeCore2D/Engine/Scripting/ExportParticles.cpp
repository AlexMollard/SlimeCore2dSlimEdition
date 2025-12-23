#include "ExportParticles.h"
#include "Rendering/ParticleSystem.h"

SLIME_EXPORT void* __cdecl ParticleSystem_Create(uint32_t maxParticles)
{
	return new ParticleSystem(maxParticles);
}

SLIME_EXPORT void __cdecl ParticleSystem_Destroy(void* system)
{
	delete (ParticleSystem*)system;
}

SLIME_EXPORT void __cdecl ParticleSystem_OnUpdate(void* system, float ts)
{
	((ParticleSystem*)system)->OnUpdate(ts);
}

SLIME_EXPORT void __cdecl ParticleSystem_OnRender(void* system)
{
	((ParticleSystem*)system)->OnRender();
}

SLIME_EXPORT void __cdecl ParticleSystem_Emit(void* system, ParticleProps_Interop* props)
{
	ParticleProps p;
	p.Position = { props->Position[0], props->Position[1] };
	p.Velocity = { props->Velocity[0], props->Velocity[1] };
	p.VelocityVariation = { props->VelocityVariation[0], props->VelocityVariation[1] };
	p.ColorBegin = { props->ColorBegin[0], props->ColorBegin[1], props->ColorBegin[2], props->ColorBegin[3] };
	p.ColorEnd = { props->ColorEnd[0], props->ColorEnd[1], props->ColorEnd[2], props->ColorEnd[3] };
	p.SizeBegin = props->SizeBegin;
	p.SizeEnd = props->SizeEnd;
	p.SizeVariation = props->SizeVariation;
	p.LifeTime = props->LifeTime;

	((ParticleSystem*)system)->Emit(p);
}
