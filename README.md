# Companion Sabotage System

**Companion Sabotage System** introduces a strategic espionage layer to Mount & Blade II: Bannerlord. This module expands the utility of companions, allowing players to deploy them as covert agents to infiltrate enemy settlements, disrupt logistics, and destabilize defenses from within.

## Overview

In native gameplay, companions are primarily used as soldiers, caravan leaders, or governors. This mod provides a fourth utility: **Espionage**.

Instead of launching a direct siege, players can send skilled agents to weaken a target beforehand. This creates new tactical opportunities, such as starving out a garrison by destroying food stocks or lowering settlement security to incite rebellion.

## Features

### 🕵️ Covert Operations
* **Infiltration Menu:** A new option, **"Send an Agent"**, is available in the menu of all enemy Towns and Castles.
* **Agent Requirements:** Not every companion is suitable for spycraft. Agents require a minimum **Roguery skill of 30** and must be healthy (HP > 40%).
* **Travel & Setup:** Operations are not instant. Agents must travel to the target (based on party distance) and spend time infiltrating local defenses before sabotage begins.

### ⚔️ Sabotage Mechanics
Once an agent has successfully infiltrated a settlement, they perform daily sabotage operations:
* **Food Supply Disruption:** Agents destroy food stocks daily. The amount destroyed scales with the agent's Roguery skill and the settlement's current reserves.
* **Destabilization:** Sabotage negatively impacts **Loyalty** and **Security**, significantly hampering the enemy's ability to maintain control or recruit noble troops.

### ⚖️ Risk & Consequences
Espionage carries significant risks calculated daily based on the **Settlement Security** vs. **Agent Roguery**.
* **Capture Logic:** If an agent fails a security check, they are captured immediately.
* **Imprisonment:** Captured agents are stripped of their role and imprisoned in the settlement's dungeon. They must be ransomed, broken out, or allowed to escape naturally.
* **Progression:** Successful daily operations award Roguery XP, allowing specialized companions to become master spies over time.

## Configuration

This mod is fully integrated with **Mod Configuration Menu (MCM)** but does not strictly require it.

**Supported Settings:**
* **XP Gain:** Adjust the experience gained per successful sabotage tick.
* **Capture Probability:** Modify the risk calculation multiplier.
* **Travel Speed:** Adjust how fast agents move between the player party and targets.
* **Sabotage Intensity:** Configure the base amount of food destroyed.

*Note: If MCM is not installed, the mod will automatically revert to default balanced values without errors.*

## Installation

### Requirements
* Mount & Blade II: Bannerlord (Compatible with latest stable branch)
* [Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006)
* [Mod Configuration Menu](https://www.nexusmods.com/mountandblade2bannerlord/mods/612) (Optional, but recommended)

### Setup
1.  Download the latest release.
2.  Extract the `CompanionSabotageSystem` folder into your game's `Modules` directory:
    `.../Mount & Blade II Bannerlord/Modules/`
3.  **Important:** Right-click `CompanionSabotageSystem.dll` -> **Properties** -> Check **Unblock** if applicable (Windows security).
4.  Enable the mod in the Bannerlord Launcher.

### Load Order
1.  Harmony
2.  Native modules (Native, Sandbox, etc.)
3.  ...
4.  **Companion Sabotage System**

## Localization

The mod uses a robust localization system with unique alphanumerical string IDs to prevent conflicts.
* **English:** Native support (default).
* **Français:** Full translation included.

*Translators: Please refer to `ModuleData/Languages/std_CompanionSabotageSystem_strings.xml` for the English template.*

## Development & Source

The project follows a clean architecture separating the source code from the build output.
* **Source Code:** Located in the `src/` directory.
* **License:** MIT License.

## Credits

* **Author:** Jules Noël--Audoux
* **License:** MIT

---
*For bug reports or suggestions, please open an issue on the repository or the Nexus Mods page.*
