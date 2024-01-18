# Coyote Time
Adds a small window of time after walking off a platform before the player starts falling. Config default is 0.3s. Barely tested.

## Issues
* Mod works off the authority of the character
	* Meaning that if PlayerA sets their config time to 1s, while PlayerHost is set to 0.3s, PlayerA's coyote time will last 1s.
	* I've clamped the max value to 0.6 seconds to try to keep it reasonable.
* Character hovers for too long if the movement speed gets high enough.


> Written with [StackEdit](https://stackedit.io/).