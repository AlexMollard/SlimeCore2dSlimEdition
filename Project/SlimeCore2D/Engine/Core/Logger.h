#pragma once
#include <string>
#include <fstream>
#include <mutex>

enum class LogLevel
{
    Trace,
    Info,
    Warn,
    Error
};

class Logger
{
public:
    static void Init();
    static void Log(LogLevel level, const std::string& message);
    
    static void Trace(const std::string& message) { Log(LogLevel::Trace, message); }
    static void Info(const std::string& message) { Log(LogLevel::Info, message); }
    static void Warn(const std::string& message) { Log(LogLevel::Warn, message); }
    static void Error(const std::string& message) { Log(LogLevel::Error, message); }

private:
    static std::ofstream s_FileStream;
    static std::mutex s_Mutex;
};
