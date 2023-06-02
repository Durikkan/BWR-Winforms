So the original goal with this one was to make an approachable, simplistic, but mostly realistic boiling water reactor simulator.  Currently it kind of assumes you know what you're doing, which I'll probably address in the future.

It is a close approximation of reality, but should not be taken as completely realistic because it takes a variety of shortcuts and approximation formulas.   Part of that was because the math gets very complicated, part of it was a lack of available data, so things like the pipe lengths are guesses.

There are places that deviate signifcantly from reality in an intentional manner, most notably in the allowed heat up rate for systems.  This was done to take the startup from a multi-hour process to something that can be done in a reasonable time frame.  The turbine is the only system that requires controlled warming, and that heats significantly faster than reality.   The condenser also evacuates air much faster than reality, as well.  Some of the automatic scrams / turbine trips are relaxed slightly to make them more managable.  The flow from the condenser after the low pressure feedwater heaters and the flow from the high pressure feedwater heater drain tank have been abstracted together to just feedwater suction.  

The rest should largely be a close approximation of reality, using exact formulas in some cases, and approximations in others.  Though it's also assuming that I've understood everything correctly, which may not be 100% the case.  The current UI is admittedly a bit of a mess, but may be entirely reworked at some point in the future.   

Known issues:
The auto-controls are a bit wonky still, they should do a reasonably good job of holding, but can sometimes overshoot, or even cause SCRAMs in some cases.  It may be a good idea to temporarily turn off the protection logic if you're going to use autos from some distance away from the setpoint.  

Starting guide:

Start the CAR pumps, and optionally the SJAEs at this stage to create a vacuum in the condenser.

Remove the rods until criticality is established, then keep the period around 30 seconds to keep it powering up at a controlled rate.  When you approach that point you'll want to slow the rod speed down so that the prompt jump from moving the rods doesn't call the period to fall too much.

Higher power will start up faster, but too high will overwhelm the cleaning pumps which are the main thing keeping the water level under control until steam is being consumed
Stablize it at a reasonable level and continue pulling rods to keep power steady wait for the coolant to boil.

Open the MSIV, and potentially the bypass slightly to help keep the coolant level down while getting up to operating pressure.

Engage the turbine turning gear, and open the turbine valve to put steam in to get it turning.  Ideally hold it around 500 rpm while waiting for the turbine to slowly warm up to avoid exceeding differential expansion limits.  This simulation also allows ramping it up for faster warming. 

When in within 2 rpm of 1800, sync the turbine.

When the coolant level falls below the ideal point in the reactor, open the feedwater valves to keep the level constant, and open the condenser valves to keep the feedwater suction levels up.  Careful about opening the values too fast, espesically with low turbine output, the cool water flowing into the reactor will likely cause the reactivity to spike.  

From there, just continue raising the power and keeping things stable until you reach the reactor's full output of 3.926 GW (3926 MW)
