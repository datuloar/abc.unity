<p align="center">
    <img src="GitResources~/img.jpg" width="656" height="239" alt="ABC logo">
</p>

# ABC - Actor Behaviour Component: A Simple C# Framework for Game Development!
This framework was developed as part of a project where ECS (Entity-Component-System) is being studied. As practice shows, it is not learned as quickly as one would like. This framework is designed to significantly accelerate the learning process for the team and make the code more flexible and readable!

The framework allows for the development of games of various scales, from simple hyper-casual games to large mid-core projects.

# License
The framework is released under the MIT License. [Details here](./LICENSE).

# Integration with Game Engine

## Unity
> Tested on Unity 2021.3 (not dependent on it) and includes asmdef descriptions for compiling into separate assemblies, reducing recompilation time for the main project.

# Installation

## As a Unity Module
Installation is supported as a Unity module via a Git link in the PackageManager:
```
"com.abc.unity": "https://github.com/datuloar/abc.unity.git",
```
By default, the latest release version is used. If a "development" version with current changes is required, switch to the `develop` branch:
```
"com.abc.unity": "https://github.com/datuloar/abc.unity.git#develop",
```

# Core Types

## Actor
An Actor is a container for components and behaviors, and can be placed in the scene as a MonoBehaviour. An Actor can send commands that are listened to only by Behaviours.

> **ВАЖНО!** IMPORTANT! It must always be present where there are Behaviours and Components.

## Key Features:
* Lifecycle Management: Actors support initialization and cleanup processes, ensuring proper resource management during their lifecycle.
* React Property System: The Actor utilizes ActorReactProperty to manage state changes, allowing for reactive programming patterns.

## Component or Data
This is regular public data processed by Behaviours:
```c#
class JumpData : IActorData {
    public int Force;
}
```

## Behaviour
Behaviours handle all the logic. They exist as custom classes implementing at least one of the IActorBehaviour interfaces. Functionality can be extended by adding several interfaces:

* `ITickable` & `IFixedTickable` & `ILateTickable`: Used for updating logic.
* `IActorCommandListener`: Receives commands from the Actor and can also send its own commands.

JumpBehaviour is a class that implements the IActorBehaviour and IActorCommandListener<JumpCommand> interfaces. It is responsible for handling jump commands and applying the corresponding physical forces to the actor object.
```c#
public class JumpBehaviour : MonoBehaviour, IActorBehaviour, IActorCommandListener<JumpCommand>
{
    public IActor Owner { get; set; }

    public void ReactActorCommand(JumpCommand command)
    {
        if (!Owner.TryGetData<JumpData>(out var jumpData))
            return;

        if (!Owner.TryGetData<RigidbodyPhysicsData>(out var physicsData))
            return;

        var force = new Vector3(0, jumpData.Force);
        physicsData.AddForce(force);
    }
}
```

## Key Functions:
* Owner: The Owner property provides access to the parent actor, allowing the JumpBehaviour class to interact with its data and behaviors.

* ReactActorCommand: The ReactActorCommand method processes the JumpCommand. Within the method:

* It attempts to retrieve jump data (JumpData) from the owner. If the data is unavailable, the method execution is halted.
* It attempts to retrieve physics data (RigidbodyPhysicsData) from the owner. If the data is unavailable, the method execution is halted.
* If both sets of data are successfully retrieved, a jump force vector is created, which is then applied to the object using the AddForce method from the physics data.

## Additional Functionalities:
* Command System: The Actor can send commands to listeners implementing IActorCommandListener, enabling decoupled communication between Actors and Behaviours.
* Blueprint System: Actors can be configured using blueprints, allowing for reusable setups of data and behaviours.

# TODO:
* Complete the documentation.
* Simplify working with Blueprints.
* Add the ability to pull components with a button.
* Introduce multithreading.
* Optimization