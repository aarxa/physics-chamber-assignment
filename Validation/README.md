# Automated validation and physics measurements
Run with Unity **6000.3.24f1**, September 29, 2026. **11 Play Mode tests passed; 0 failed.** These automated tests and measurements provide technical validation of the scene.
## Physics experiment
Only Payload mass changes between the three trials. Actor mass stays 1 kg, Actor linear damping 4, Payload linear damping 0.5, and driving force 20 N. Contact material stays at static friction 0.15 and dynamic friction 0.12 with Minimum combine and zero bounce.
Each trial starts the Actor at (-4.05, 0.5, -6) and the Payload at (-3, 0.5, -6), with both velocities reset. The test applies the same +X force for 100 physics steps of 0.02 seconds (2 simulated seconds). It measures Payload travel, final speed, and travel during the second second, which checks that pushing continues after the initial contact. The test uses an accelerated clock for execution speed; the table reports simulated time, not human play time.
| Payload mass (kg) | Travel in 2 s (m) | Final speed (m/s) | Travel during second second (m) |
| ---: | ---: | ---: | ---: |
| 0.5 | 5.883 | 3.534 | 3.485 |
| 1 | 4.867 | 3.058 | 2.972 |
| 3 | 2.281 | 1.581 | 1.468 |

Raw output: [physics-experiment.csv](physics-experiment.csv). The saved gameplay scene restores Payload mass to **1 kg**.

The measurement helper applies forces to the Actor in physics steps. It does not move the Payload by setting its Transform. Initial placements/resetting for isolated tests are test setup, not gameplay mechanics.

## Tests

| Test | Result |
| --- | --- |
| `ActorCannotCountAsDeposit` | Passed |
| `ActorUnlocksRealDoorAndLogsOnlyOnce` | Passed |
| `ContinuousPushAndRecordedMassExperiment` | Passed |
| `DoorPhysicallyBlocksActorBeforeUnlock` | Passed |
| `EarlyDepositIsRejectedAndMustReenterAfterUnlock` | Passed |
| `FullRoutePushesPayloadThroughBothTurnsAndDeposits` | Passed |
| `PayloadCannotActivateSensor` | Passed |
| `RestartResetsDoorPayloadAndSuccess` | Passed |
| `SuccessFiresOnceAndTimerIncludesTimeBeforeUnlock` | Passed |
| `WasdKeysMoveInAllFourDirections` | Passed |
| `WrongPayloadTagCannotCountAsDeposit` | Passed |

The full-route test starts from the saved scene and applies forces to the Actor to push the Payload around both maze turns, cross the sensor, and deposit. It never teleports the Payload. The WASD test injects virtual keyboard input, temporarily overrides batch-mode focus routing, and restores the settings afterwards.

## Reproduce

Open **Window → General → Test Runner → PlayMode** and run `ChamberPlayModeTests`. The mass experiment rewrites its CSV automatically; the saved scene is not changed by these Play Mode trials.

For the student reflections, play the chamber yourself, make your own notes, and write your own answers. No reflection prose has been generated.
