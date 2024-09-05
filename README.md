# ForeverBackpack
## Maintain Rust backpack contents across wipes

1. At each player disconnect, check their worn items for a Rust backpack.
2. If present, save the contents to a data file.
3. On each player connect, check their inventory.  If the wear container is empty, attempt to populate a backpack and refill it from the data file.
4. Add the player steamid to a list, saved in another data file.  This file gets cleared on wipe so that the player will only get a refill once per wipe.

If the player dies without their backpack in inventory, it will get dropped per the default server settings.

NOTE: If you see that players are abusing the refilling of a backpack somehow, please let me know.  The goal is to reload once per wipe, and that should be working.

At wipe, the player's wear inventory should be empty.  A new backpack will be added along with the contents if the player played the previous month with a backpack at any time.

### Configuration
```json
{
  "Options": {
    "UseLargeBackpack": true,
    "AlwaysIssueBackpack": false,
    "RequirePermission": false,
    "debug": true
  },
  "Version": {
    "Major": 0,
    "Minor": 0,
    "Patch": 3
  }
}
```

- `UseLargeBackpack` -- (Default true) Use the large Rust backpack instead of the small backpack
- `AlwaysIssueBackpack` -- (Default false) Issue backpack for a new player and equip in their wear container
- `RequirePermission` -- (Default false) Require foreverbackpack.use permission to use.

### Permissions

 - `foreverbackpack.use` -- If RequirePermission is true, this is what they need.

## Commands

No commands.

## Work in progress

1. Verify that ammoCount is maintained.
