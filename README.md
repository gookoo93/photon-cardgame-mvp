# Photon Cardgame MVP

## Host-Authoritative Multiplayer Architecture Sample

Unity + Photon PUN2

A minimal 2-player turn-based card game built to demonstrate:

-   Correct multiplayer synchronization
-   Authority separation
-   Reliable hidden-information handling

This project prioritizes network correctness and architectural clarity
over visuals and content scale.

------------------------------------------------------------------------

## Project Purpose

This project is a network-architecture portfolio sample, not a
content-heavy game.

Primary goals:

-   Strict Host-Authoritative multiplayer model
-   Deterministic result calculation
-   Hidden information protection
-   Controlled state synchronization
-   Reconnect grace handling
-   Clear separation between networking logic and UI

------------------------------------------------------------------------

## Core Networking Philosophy

### Authority Model

This project follows a strict Host-Authoritative architecture.

#### MasterClient (Host)

-   Calculates match results
-   Updates scores
-   Advances phase state
-   Controls reconnect grace timer
-   Decides match termination

#### Clients

-   Send input only
-   Never calculate results locally
-   Update UI exclusively through RPC broadcasts

### What This Prevents

-   Score divergence
-   Double execution bugs
-   Latency-based desynchronization
-   Client-side cheating risks

------------------------------------------------------------------------

## Hidden Information Handling

Card submission and reveal are intentionally separated.

### Submission Phase

-   Each client sends only their selected card via RPC
-   Opponent card remains visually hidden

### Reveal Phase

-   Host triggers synchronized reveal RPC
-   Both cards are revealed simultaneously

Opponent card data is never exposed before reveal.

------------------------------------------------------------------------

## Phase Synchronization Strategy

### State Machine

WaitingForPlayers\
→ Round_Pick\
→ Round_Reveal\
→ Round_Result\
→ Winner_Choose\
→ Game_End

All phase transitions are initiated by the Host.

### Synchronization Tools

-   RPC_BroadcastReveal
-   RPC_BroadcastResult
-   RPC_SyncStateToRejoin
-   PhotonNetwork.Time (phase timing reference)

Clients are passive state receivers.

------------------------------------------------------------------------

## Reconnect and Disconnect Handling

### Guest Disconnect

-   Host starts a 60-second grace timer
-   If the guest reconnects:
    -   Full state sync via RPC_SyncStateToRejoin
-   If timeout expires:
    -   Game resets via RPC_ForceResetToWaiting

### Host Disconnect

-   Authority cannot be preserved
-   OnMasterClientSwitched triggers exit
-   Client returns to Lobby
-   UI informs the user

This avoids undefined authority states.

------------------------------------------------------------------------

## Architecture Overview

### Scene Flow

PhotonBootstrap (DontDestroyOnLoad) ↓ TitleManager ↓ LobbyManager ↓
GameManager (Host Authority)

------------------------------------------------------------------------

## Key Components

### PhotonBootstrap

-   Connection handling only
-   Auto-reconnect on unexpected disconnect
-   AutomaticallySyncScene enabled

### LobbyManager

-   Room list caching
-   Search and filter
-   Private / public room handling
-   CustomProperties usage (title, code, password)

### GameManager

-   State machine
-   Authority separation
-   Deterministic result calculation
-   Reconnect logic
-   Winner role selection logic

### HandCardButton

-   Card selection logic
-   Dynamic key-card replacement
-   Visual state handling

------------------------------------------------------------------------

## Game Rules (Simplified)

Each player has 5 cards.

### King Deck

-   1 King
-   4 Citizens

### Slave Deck

-   1 Slave
-   4 Citizens

Rules:

-   King beats Citizen
-   Citizen beats Slave
-   Slave beats King

First player to reach 10 points wins.

------------------------------------------------------------------------

## What This Demonstrates

This project demonstrates my ability to:

-   Design multiplayer authority structures
-   Prevent client-side desynchronization
-   Handle state recovery
-   Separate UI from game logic
-   Build deterministic match systems
-   Handle disconnection safely
-   Use Photon PUN beyond basic room join

------------------------------------------------------------------------

## How To Run

1.  Open in Unity
2.  Install Photon PUN2
3.  Insert your Photon App ID
4.  Run two clients (Editor + Build or ParrelSync)

------------------------------------------------------------------------

## Future Improvements

-   Host migration architecture research
-   Server-authoritative model comparison
-   Spectator mode
-   Better timer visualization
-   Event-driven UI refactor
