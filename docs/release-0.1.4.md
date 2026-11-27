# Weekly Hermit 0.1.4 Verification

The portfolio run and controller approval covered the 0.1.3 candidate on Super Fantasy Kingdom 0.8.070.

| Artifact | SHA-256 |
| --- | --- |
| Approved 0.1.3 ZIP | `06a3f13b10fcd07e833ff972fda764c8dfec7e4a69aa5caa2e1e0e02c7064694` |
| Approved 0.1.3 DLL | `b91f7e750e48801824bed00824d51af79473c6aa91038677592f9baa4d4c2d57` |
| 0.1.4 release DLL | `862e913c4953eb4716dfbd243d0700c531c9e9dc7a50ba2ebab9ce4099ac4c02` |

Thunderstore already served a different DLL as 0.1.3. To publish the approved implementation as 0.1.4, the release DLL was produced directly from the approved DLL by updating its assembly, file, informational and BepInEx version metadata. A Mono.Cecil comparison covered all 31 type definitions and 78 methods, including signatures, instructions, locals, exception handlers, references and resources. The Release build from the checked-in source passed the same comparison and all 18 offline visit and recruitment checks passed.

The approved [Human](screenshots/current/human-hermit-1x.png) and [Undead](screenshots/current/undead-hermit-1p5x.png) screenshots depict the shared recruit-card layout. They were captured during a separate virtual-input UI pass on 0.8.070 using DLL SHA-256 `44edacfcdab8c57dd48a79662faa1ccfaa11e0d844e8e5182064eaef6d69e6fc`. The gameplay approval and release byte comparison use the candidate listed above.
