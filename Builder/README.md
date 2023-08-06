# Welcome to the Santroller Configuration Tool

This branch is for a new version of the tool that has not yet been released. Below is a list of all the new features
being added:

- A lot more is configurable, as the is now compiled to include just features you are using, which means we can support
  a LOT more features without running out of space on the arduinos
- A much cleaner and easier to work with codebase for both the firmware and the tool
- New user interface that supports a lot more configuration but is also much simpler to use
    - Most inputs are also live, which means you no longer need a separate controller tester when testing your buttons
      work.
- Configurable pins for most things (pico only)
    - this means you no longer are limited to picos that expose all the required pins, as you can now pick whatever pins
      you want for making wii adapters or ps2 adapters
- Custom button combos - instead of map start + select as an option, now any output can be mapped to combinations of any
  input.
- Better support for different types of controllers
    - Full Pro Drum support, including fixed mappings for all the other drums as well
    - Full support for the Rock Band guitars, including the solo frets
    - Full support for GHL
    - Full support for DJ Hero turntables
- When using HID on a PC, the triggers are now exposed correctly
- Stage Kit emulation support
- Support for using USB devices as inputs
    - pi pico only, as this is the only support microcontroller that can do this sort of thing
    - Also support for using USB devices for handling authentication for use on retail consoles
    - This will work by hooking up the usb pins from the input controller to a few pins on the pico.
- Automatic console / OS detection
    - No more requirement to use PS3 mode for macos or linux, your controller will automatically detect what it is
      plugged into and pick the best emulation mode for that device.
    - You can also set button combinations to force specific modes, if you are for example using an emulator that needs
      a specific mode.
- Support for all consoles where rhythm games apply
    - PS2 (requires
      a [nightly build of OPL](https://github.com/ps2homebrew/Open-PS2-Loader/releases/download/latest/OPNPS2LD.7z), as
      I added support for guitars myself and there has not been a stable build of OPL with those changes yet)
    - PS3 (works natively)
    - PS4/5
        - PS3 mode is used for Guitars and Drums, so it will just work
        - GHL and Gamepads require using a standard dualshock for authentication
    - Xbox 360
        - The RGH plugin will allow you to use your devices without auth
        - For retail, you can use any wired xbox 360 controller for auth
    - Xbox one / series s / series x
        - You will need to use a controller for auth
    - Wii
        - Rock Band guitars / drums just work as they are native usb
        - Guitar Hero guitars / drums and DJ Hero turntables will be usable via a fork of a CIOS module I am working on
          called [fakemote](https://github.com/sanjay900/fakemote)
- Overhaul of LEDs
    - You can pick events and then bind to them, including things like stage kit led events, or player led events. If
      you are using APA102s then any led can be bound to any effect, but you can also bind effects to a digital output.
    - It is now also possible to bind multiple things to a single LED, and pick the LED off colour as well, which allows
      for all sorts of crazy things
- RF support is being removed
    - There are just too many dodgy NRF modules out there, and supporting this is not something I can easily do. I
      require things like ACKs and a lot of the knock-off NRF modules don't implement them correctly.
- Bluetooth support (on the pi pico w)
    - You can build a bluetooth transmitter that will just work on computers
    - You can also build a bluetooth receiver and connect it to a transmitter, for console support