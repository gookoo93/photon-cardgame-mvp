# Photon Cardgame MVP

A minimal turn-based card game MVP built with **Unity + Photon PUN2**  
to demonstrate **host-authoritative networking**, **server-time synchronization**,  
and **hidden information handling**.

## Overview
This project is not focused on visuals or game polish.  
Its purpose is to clearly demonstrate reliable multiplayer turn synchronization
and deterministic result calculation in a small, controlled scope.

## Key Features
- Host-authoritative turn and result calculation
- Server-time based phase synchronization (PhotonNetwork.Time)
- Hidden information handling (opponent card is never sent before reveal)
- Deterministic scoring logic
- Basic disconnect handling

## Game Rules (Simplified)
- Each player has 5 cards:
  - King Deck: 1 King, 4 Citizens
  - Slave Deck: 1 Slave, 4 Citizens
- King always beats Citizens  
- Citizens beats Slave  
- Slave beats King
- First player to reach **10 points** wins the match

## Turn Flow
1. Place Phase: Players select and submit a card
2. Reveal Phase: Host reveals both cards simultaneously
3. Score Phase: Host calculates and syncs scores
4. Next Phase: Prepare next turn

## Architecture Overview
- **GameNetworkManager**: Photon connection and room handling
- **TurnCoordinator (Host Only)**: Phase control, timer, result calculation
- **MatchState / PlayerState**: Deterministic game state data
- **UIController**: Card selection and state display
- **DebugOverlay**: Network and turn state visualization

## Why This Project Exists
This project was created as a **networking-focused portfolio sample**.
Visual quality and content scale were intentionally minimized
to focus on correctness, clarity, and reliability.

## How to Run
1. Open the project in Unity
2. Install Photon PUN2
3. Set your Photon App ID
4. Build & run two clients or use Editor + Build

## Status
- MVP in progress

---

[Korean documentation is available in README_KR.md]