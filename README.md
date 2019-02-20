# itinero-intermodal

An intermodal routing module for Itinero. The focus of this project is to provide a good collection of algorithms to plan intermodal trips.

The initial target is to be able to:

- Plan basic intermodal trips.
- Combining transit and pedestrian/car/bike.
- Combine multiple transit operators.

## Approach

We plan to combine the current Itinero routerdb concept and one or more transitdbs into one intermodal data structure. The goal is to build a data structure that can be filled with data on-the-fly but also caching data that can be reused. We need a data structure that is:

- A highly optimized data structure for route planning.
- A data structure that can be serialized to disk and accessed via memory-mapping.
- A data structure that can be updated while route planning is happening.

For intermodal support we also need to add:

- Concept of transfers (either precalculated or calculated on-the-fly and cached).
  - Transfers can be between different transit dbs.
  - Transfers can also be between stops of the same transit db.
  - Transfers are done via the road network with a specific profile.
  - We only need to cache data needed during route planning (not the entire routes).

