Photon Cardgame MVP

Host-Authoritative Multiplayer Architecture Sample (Unity + Photon PUN2)

A minimal 2-player turn-based card game built to demonstrate correct multiplayer synchronization, authority separation, and reliable hidden-information handling in a small but technically rigorous scope.

This project prioritizes network correctness and architecture clarity over visuals and content scale.

🎯 Project Purpose

This project exists as a network-architecture portfolio sample, not as a content-heavy game.

The primary goal was to design and implement:

A strict Host-Authoritative multiplayer model

Deterministic result calculation

Hidden information protection

Controlled state synchronization

Reconnect grace handling

Clean separation between networking logic and UI

🧠 Core Networking Philosophy
Authority Model

This project follows a strict Host-Authoritative architecture.

Only the MasterClient (Host):

Calculates match results

Updates scores

Advances phase state

Controls reconnect grace timer

Decides match termination

Clients:

Send input only

Never calculate results locally

Update UI exclusively through RPC broadcasts

This eliminates:

Score divergence

Double execution bugs

Latency-based desync

Client-side cheating risks

🔐 Hidden Information Handling

Card submission is intentionally separated from reveal.

During submission:

Each client sends only their selected card via RPC.

Opponent card remains visually hidden.

During reveal:

Host triggers a synchronized reveal RPC.

Both cards are revealed simultaneously.

Opponent card information is never exposed before reveal.

This ensures deterministic fairness and information integrity.

🔄 Phase Synchronization Strategy

Game phases:

WaitingForPlayers
→ Round_Pick
→ Round_Reveal
→ Round_Result
→ Winner_Choose
→ Game_End

All phase transitions are initiated by the Host.

Synchronization tools:

RPC_BroadcastReveal

RPC_BroadcastResult

RPC_SyncStateToRejoin

PhotonNetwork.Time (phase timing reference)

Clients are passive state receivers.

🔁 Reconnect & Disconnect Handling
Guest Disconnect

Host starts a 60-second grace timer.

If the guest reconnects:

Full state sync is performed via RPC_SyncStateToRejoin.

If timeout expires:

Game resets to Waiting state via RPC_ForceResetToWaiting.

Host Disconnect

Since authority cannot be preserved:

OnMasterClientSwitched triggers immediate exit.

Client returns to Lobby.

UI informs the user.

This avoids undefined authority states.

🧩 Architecture Overview

Scene Flow:

PhotonBootstrap (DontDestroyOnLoad)
    ↓
TitleManager
    ↓
LobbyManager
    ↓
GameManager (Host Authority)
Key Components

PhotonBootstrap

Handles connection only

Auto-reconnect on unexpected disconnect

AutomaticallySyncScene enabled

LobbyManager

Room list caching

Search/filter

Private/public room handling

CustomProperties usage (title/code/password)

GameManager

State machine

Authority separation

Deterministic result calculation

Reconnect logic

Winner role selection logic

HandCardButton

Card selection logic

Dynamic key-card replacement

Visual state handling

🎮 Game Rules (Simplified)

Each player has 5 cards:

King Deck:

1 King

4 Citizens

Slave Deck:

1 Slave

4 Citizens

Rules:

King beats Citizen

Citizen beats Slave

Slave beats King

First player to reach 10 points wins.

🧪 What This Demonstrates

This project demonstrates my ability to:

Design multiplayer authority structures

Prevent client-side desynchronization

Handle state recovery

Separate UI from game logic

Build deterministic match systems

Handle disconnection safely

Use Photon PUN beyond basic room join

🚀 How To Run

Open in Unity

Install Photon PUN2

Insert your Photon App ID

Run two clients (Editor + Build or ParrelSync)

📌 Future Improvements

Host migration architecture research

Server-authoritative model comparison

Spectator mode

Better timer visualization

Refactor to event-driven UI update

📣 Final Note

This project intentionally avoids visual complexity to highlight multiplayer correctness and architectural discipline.

It is a focused demonstration of reliable 2-player real-time turn synchronization.
