# aria2 sidecar

Place the bundled aria2 executable here when shipping OmniDown.

Recommended Windows layout:

```text
Engines/
  aria2/
    win-x64/
      aria2c.exe
    win-arm64/
      aria2c.exe
```

Users can still override this in Settings with a custom `aria2c.exe` path.
