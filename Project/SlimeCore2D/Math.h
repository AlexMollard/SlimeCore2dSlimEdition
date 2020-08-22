#pragma once
#include "glm.hpp"

namespace Math
{
	static float Lerp(float pointOne, float pointTwo, float time)
	{
		time = (time < 0) ? 0 : (time > 1) ? 1 : time;
		return pointOne + time * (pointTwo - pointOne);
	}

	static glm::vec2 Lerp(glm::vec2 pointOne, glm::vec2 pointTwo, float time)
	{
		time = (time < 0) ? 0 : (time > 1) ? 1 : time;
		return pointOne + time * (pointTwo - pointOne);
	}

	static glm::vec3 Lerp(glm::vec3 pointOne, glm::vec3 pointTwo, float time)
	{
		time = (time < 0) ? 0 : (time > 1) ? 1 : time;
		return pointOne + time * (pointTwo - pointOne);
	}

	static glm::vec4 Lerp(glm::vec4 pointOne, glm::vec4 pointTwo, float time)
	{
		time = (time < 0) ? 0 : (time > 1) ? 1 : time;
		return pointOne + time * (pointTwo - pointOne);
	}

	static glm::vec2 BezierCurve(glm::vec2 pointOne, glm::vec2 pointTwo, glm::vec2 pointThree, float time)
	{
		glm::vec2 m1 = Lerp(pointOne, pointTwo, time);
		glm::vec2 m2 = Lerp(pointTwo, pointThree, time);
		return Lerp(m1, m2, time);
	}

	static glm::vec2 BezierCurve(glm::vec2 pointOne, glm::vec2 pointTwo, glm::vec2 pointThree, glm::vec2 pointFour, float time)
	{
		glm::vec2 m1 = Lerp(pointOne, pointTwo, time);
		glm::vec2 m2 = Lerp(pointTwo, pointThree, time);
		glm::vec2 m3 = Lerp(pointThree, pointFour, time);
		return BezierCurve(m1, m2, m3, time);
	}
	
};