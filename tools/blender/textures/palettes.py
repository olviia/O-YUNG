"""Shared color ramps, so related textures stay one color family.

Format: [(position 0..1, (r, g, b)), ...] in sRGB, dark -> light.
Stylized rule: shadows shift HUE (toward red/violet brown), highlights toward warm yellow -
never just "the same color, darker".
"""

WICKER = [                  # dried willow, sampled from the nursery concept
    (0.00, (0.36, 0.22, 0.18)),   # occluded: reddish-violet brown
    (0.35, (0.55, 0.36, 0.21)),   # strand edge
    (0.75, (0.76, 0.57, 0.34)),   # strand body
    (1.00, (0.92, 0.79, 0.54)),   # painted highlight: warm straw
]

WICKER_STAKE = [            # vertical stakes: a separate, adjustable family (a touch deeper/redder)
    (0.00, (0.33, 0.19, 0.15)),
    (0.35, (0.50, 0.31, 0.19)),
    (0.75, (0.68, 0.48, 0.30)),
    (1.00, (0.84, 0.68, 0.46)),
]
