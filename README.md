# OpenRadar

A FOSS solution for small-scale multiplayer with simulated/roleplay atc services for Microsoft Flight Simulator 2020.
Not for any real world aviation use.

Consists of two major components, the simulator connector and the radar client. The sim connector connects to a locally
running instance of MSFS2020 and retrieves needed data about the flight plan, which is then sent over a tcp/tls
connection to the radar client, which shows the simulator's plane on the radar display. 
Details of the area for the radar display are loaded from a standard VATSIM-format sector file.