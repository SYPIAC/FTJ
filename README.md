#Desperate Gods (Unnamed Mod)

## Implemented

- Now using Unity 4
- Ability to pick up multiple items / entire decks (using right click)
- Some balance changes (to be summarized based on commits later)

## Goals

In no particular order:

- Move board definition and resources outside of Unity to allow for easier mods (WIP)
- Add additional mouse-based controls; all basic actions should be available with only mouse (ex. mouse wheel to rotate)
- Add more objects to game. Additional dice (ex. d20), paper to write on, etc.
    - Perhaps some sort of "infinite" piles to supply dice, game currency / units, etc.
- Implement some sort of mechanic for games with hidden information (so you can "peek" at your cards or similar). Perhaps four "fog of war" areas where each cursor can claim one and only they can enter / see what's inside?
- Additional balance changes
- Add icon to zoomed-in cursors
- Allow saving and restoring of board
- Add more host settings (ex. change password, limit "Reset Board" use)
- Add host transfer (perhaps make this automatic when host disconnects)
- Improve GUI
- Add in-game rules access (I'd like it to be an actual in-game book you can pull out, so everyone can see the rules, but a GUI element will likely be more feasible for now)
- Downgrade from Unity Pro to Unity
    - Remove use of RenderTexture (used for all tiles and cards right now)
    - Other changes (remove custom shaders?)
    - Use older dynamic shadows solution

## Progress

- Move board definition and resources outside of Unity to allow for easier mods
    - YES - Remove current board resources from project
    - YES - Create file format for mod information
    - PARTIAL - Load mod information
        - YES - Deck
        - YES - Dice
        - PARTIAL - Board
        - NO - Tokens
    - PARTIAL - Spawn objects based on mod information
        - YES - Deck
        - YES - Dice
        - NO - Board
        - NO - Tokens
    - YES - Load textures from file
        - NO - Optimize 
    - NO - Enforce identical mods for networking
    - NO - Filter by mod in server list
    - NO - Add interface to choose desired mod (or perhaps just a more involved server setup step)
    - NO - Load additional assets (sounds, models) from file
    - NO - Refactor the code

## Bugs

- Dynamic textures become glitched when focus is lost / window is moved
    - This makes all cards and tiles unreadable
    - This will likely be "fixed" by rendering all the card and tile textures and importing the fully-rendered images with the new mod loading system
- Drawing card from two-card face-up deck causes remaining card to go flying
- Camera can be panned off the board
- Dice often end up at top of screen but not flat, so they cannot be read
- Deck merging (using new right click feature) can cause decks to vanish / teleport with high latency (further testing required)
- Shadows break in upper right corner
- Assets are reloaded when you leave the server


## License

As per original license by Wolfire games:

This repository is public for convenience and for personal use, but is not licensed for redistribution in whole or in part. We are open to issuing free permissive licenses for mods or code pieces, but you will just have to get our permission first ( contact@wolfire.com ), because we were burnt in the past by public confusion about open-source licensing. ( See http://blog.wolfire.com/2011/02/Counterfeit-Lugaru-on-Apple-s-App-Store-developing for more about this ). Thank you for understanding!
