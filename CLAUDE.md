# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6000.0.58f1 project named "AgenticFlow" that uses the Universal Render Pipeline (URP). The project appears to be an AI agent framework implementation for Unity.

## Project Structure

- **Assets/Scripts/AgenticFlow/**: Contains the core AI agent framework code
  - `IAgent.cs`: Defines interfaces for agents, prompts, contexts, and actions
  - `Agent.cs`: Main agent implementation
  - `GeminiModel.cs`: Integration with Google's Gemini AI model
  - `IContextCollector.cs` & `IPromptCollector.cs`: Interfaces for collecting context and prompts

## Key Dependencies

- Unity 6000.0.58f1 (Unity 6 LTS)
- Universal Render Pipeline (URP) 17.0.4
- Unity Input System 1.14.2
- Newtonsoft JSON 3.2.1
- Unity AI Navigation 2.0.9
- Unity Visual Scripting 1.9.7

## Build and Development Commands

### Unity Editor Operations
- Open the project in Unity Hub using Unity 6000.0.58f1
- Build the project: File → Build Settings → Build
- Run in Editor: Press Play button in Unity Editor

### Command Line Build (if Unity CLI is installed)
```bash
# Build for Windows
Unity -batchmode -quit -projectPath . -buildWindows64Player Builds/Windows/AgenticFlow.exe

# Build for macOS
Unity -batchmode -quit -projectPath . -buildOSXUniversalPlayer Builds/Mac/AgenticFlow.app

# Build for Linux
Unity -batchmode -quit -projectPath . -buildLinux64Player Builds/Linux/AgenticFlow
```

### Running Tests
```bash
# Run Unity Test Framework tests
Unity -batchmode -runTests -projectPath . -testResults TestResults.xml
```

## Architecture Overview

The project implements an AI agent framework with the following key components:

1. **Interface-based Design**: Core functionality is defined through interfaces (`IAgent`, `IPrompt`, `IContext`, `IAction`) enabling flexible implementations

2. **Action System**: Actions implement `IAction<T>` with an `Execute` method for task execution

3. **Model Integration**: `IAgentModel` interface allows plugging in different AI models (currently Gemini)

4. **Type Safety**: Generic constraints ensure type safety across prompt, context, and action implementations

5. **Async/Await Pattern**: Framework uses `Task` and `CancellationToken` for asynchronous operations

## Development Workflow

1. Scripts should be placed in `Assets/Scripts/AgenticFlow/` following the established namespace pattern
2. Use the existing interface structure when adding new agent capabilities
3. Test scenes are located in `Assets/Scenes/`
4. Project uses the new Unity Input System - configure input actions in `InputSystem_Actions.inputactions`