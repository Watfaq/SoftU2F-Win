Soft U2F is a software U2F authenticator for Windows. It emulates a hardware U2F HID device and performs cryptographic operations using the DPAPI. This tool works with Google Chrome and Opera's built-in U2F implementations as well as with the U2F extensions for OS X Safari and Firefox.

We take the security of this project seriously. Report any security vulnerabilities to i#xiaoba.me

## Installing

Coming Soon.

## Usage

The app runs in the background. When a site loaded in a U2F-compatible browser attempts to register or authenticate with the software token, you'll see a notification asking you to accept or reject the request. You can experiment on [Yubico's U2F demo site](https://demo.yubico.com/u2f).

### Registration

![Registration](https://user-images.githubusercontent.com/543405/59797397-e9ab4e80-9322-11e9-9f36-555b608f926d.png)

### Authentication

![Authentication](https://user-images.githubusercontent.com/543405/59797166-6c7fd980-9322-11e9-952d-c3f353a09a65.png)

## Uninstalling

Coming Soon.

## Security considerations

This is a port of https://github.com/github/SoftU2F.

Instead of macOS Keychain, we store data using Windows DPAPI, which is designed by Microsoft Windows to store data data such as passwords, keys, and connection strings.

For more infomation of DPAPI: https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata?view=netframework-4.8#remarks

A [note](https://github.com/github/SoftU2F#security-considerations) from Github Team

## Known app-IDs/facets

Every website using U2F has an app-ID. For example, the app-ID of [Yubico's U2F demo page](https://demo.yubico.com/u2f) is `https://demo.yubico.com`. When the low-level U2F authenticator receives a request to register/authenticate a website, it doesn't receive the friendly app-ID string. Instead, it receives a SHA256 digest of the app-ID. To be able to show a helpful alert message when a website is trying to register/authenticate, a list of app-ID digests is maintained in this repository. You can find the list [here](https://github.com/ibigbug/SoftU2F-Win/blob/master/APDU/KnownFacets.cs). If your company's app-ID is missing from this list, open a pull request to add it.

## Licensing

This project is [Unlicensed](https://github.com/ibigbug/SoftU2F-Win/blob/master/LICENSE) yet.

## Credits

Lots of credits to the original work of [SoftU2F](https://github.com/github/SoftU2F) done by Github team.
