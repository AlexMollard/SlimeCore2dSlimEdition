#include "Logger.h"

#include <ctime>
#include <iomanip>
#include <iostream>
#include <windows.h>

std::ofstream Logger::s_FileStream;
std::mutex Logger::s_Mutex;

void Logger::Init()
{
	s_FileStream.open("SlimeCore.log", std::ios::out | std::ios::trunc);
}

void Logger::Log(LogLevel level, const std::string& message)
{
	std::lock_guard<std::mutex> lock(s_Mutex);

	HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);

	// Time
	std::time_t t = std::time(nullptr);
	std::tm tm;
	localtime_s(&tm, &t);

	// Color
	int color = 7; // White
	std::string levelStr;

	switch (level)
	{
		case LogLevel::Trace:
			color = 8;
			levelStr = "TRACE";
			break; // Gray
		case LogLevel::Info:
			color = 10;
			levelStr = "INFO ";
			break; // Green (Bright)
		case LogLevel::Warn:
			color = 14;
			levelStr = "WARN ";
			break; // Yellow (Bright)
		case LogLevel::Error:
			color = 12;
			levelStr = "ERROR";
			break; // Red (Bright)
	}

	// Print to Console
	SetConsoleTextAttribute(hConsole, 8); // Gray for time
	std::cout << "[" << std::put_time(&tm, "%H:%M:%S") << "] ";

	SetConsoleTextAttribute(hConsole, color);
	std::cout << "[" << levelStr << "] ";

	SetConsoleTextAttribute(hConsole, 7); // Reset to white
	std::cout << message << "\n";

	// Print to File
	if (s_FileStream.is_open())
	{
		s_FileStream << "[" << std::put_time(&tm, "%H:%M:%S") << "] [" << levelStr << "] " << message << std::endl;
		s_FileStream.flush();
	}
}
