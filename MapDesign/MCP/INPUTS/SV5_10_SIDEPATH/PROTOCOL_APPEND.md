
## SV5_10_SIDEPATH — sector-local sidepath evidence

- SIDE_PATH length is the count of distinct ordered traversable AIR centerline cells, endpoints included.
- Candidate discovery uses one 48x32 sector index and eligible AABB-near sector pairs; it does not copy or rescan 624x416 per candidate.
- Final global topology and 9-state x 6-order validation runs once after deterministic packing.
- Generated diagnostics under `_work` are disposable and excluded from Result commits and Review ZIPs.
