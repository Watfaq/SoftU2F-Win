Soft U2F is a software U2F authenticator for Windows. It emulates a hardware U2F HID device and performs cryptographic operations using the DPAPI. This tool works with Google Chrome. Running on other browsers hasn't been tested.

We take the security of this project seriously. Report any security vulnerabilities to i#xiaoba.me

[![Build Status](https://watfaq.visualstudio.com/SoftU2F/_apis/build/status/ibigbug.SoftU2F-Win?branchName=master)](https://watfaq.visualstudio.com/SoftU2F/_build/latest?definitionId=7&branchName=master)

## Installing

> **This app is still under very active development. It may have bugs or doesn't work in some scenarios. Please don't use for production.**


### Prerequisites

* Disable [Driver Signing Enforcement](https://docs.microsoft.com/en-us/windows-hardware/drivers/install/kernel-mode-code-signing-policy--windows-vista-and-later-)

  To install the driver, you'll need to disable the driver signing enforcement.

  The easiest way to do this is putting you device into Test Mode. Run this in elevated prompt

  ```
  $ bcdedit /set TESTSIGNING OFF
  ```

  More ways to [disable the enforcement](https://windowsreport.com/driver-signature-enforcement-windows-10/)

### Driver

1. Download the latest driver release at [Driver Release](https://github.com/ibigbug/SoftU2F-Win/releases/tag/driver-06212019)

2. Run the `driver-install.ps1` in elevated powershell.

### Daemon

1. Download

  * If you have latest .NET Core 3.0 SDK/Runtime installed you can download the FDE(Framework dependent executable) at [Daemon Release](https://github.com/ibigbug/SoftU2F-Win/releases/tag/daemon-alpha-release)
  * Otherwise you can download the SCD(Self-contained deployment) at [Daemon Release](https://github.com/ibigbug/SoftU2F-Win/releases/tag/daemon-alpha-release)

2. Extrat and Run SoftU2FDaemon.exe

## Usage

The app runs in the background. When a site loaded in a U2F-compatible browser attempts to register or authenticate with the software token, you'll see a notification asking you to accept or reject the request. You can experiment on [Yubico's U2F demo site](https://demo.yubico.com/u2f).

### Registration

![Registration](https://user-images.githubusercontent.com/543405/59797397-e9ab4e80-9322-11e9-9f36-555b608f926d.png)

### Authentication

![Authentication](https://user-images.githubusercontent.com/543405/59797166-6c7fd980-9322-11e9-952d-c3f353a09a65.png)

## Uninstalling

### Driver

1. Right Click the Windows logo on you status bar and open Device Manager
2. Under Human Interface Devices, find **SoftU2F Device**, right click and select **Uninstall Device**

### Daemon

1. Exit App
2. Delete the folder where you extracted them.

## Security considerations

This is a port of https://github.com/github/SoftU2F.

Instead of macOS Keychain, we store data using Windows DPAPI, which is designed by Microsoft Windows to store data data such as passwords, keys, and connection strings.

For more infomation of DPAPI: https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata?view=netframework-4.8#remarks

A [note](https://github.com/github/SoftU2F#security-considerations) from Github Team

## Signing

Announced by Microsoft,

> Note  Windows 10 for desktop editions (Home, Pro, Enterprise, and Education) and Windows Server 2016 kernel-mode drivers must be signed by the Windows Hardware Dev Center Dashboard, which requires an EV certificate. For details, see [Driver Signing Policy](https://docs.microsoft.com/en-us/windows-hardware/drivers/install/kernel-mode-code-signing-policy--windows-vista-and-later-).

Windows will only trust the drivers signed by a [trusted EV certificate](https://docs.microsoft.com/en-us/windows-hardware/drivers/dashboard/get-a-code-signing-certificate#step-2-buy-a-new-code-signing-certificate).

At this stage, I'm still trying to get a EV Certificate to sign this driver, so that Disabling driver signature enforcement won't be needed to run this software.

Having a signature won't change any of the behaviour of this software and all the source code is public to everyone to read and contribute.

## Support

If you like this project, you can support me to buy a EV certificate, or just a cup of coffee :)

* [`PayPal`](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=4HZETSUYU29T8&currency_code=USD&source=url)

* [`₿TC`](https://www.blockchain.com/btc/payment_request?address=14WABfFsMR51oP5LgJZEzSP5dLoBxymop3&message=Support+SoftU2F)

* [`EOS`](https://eosauthority.com/account?account=eosgolangsdk&network=eos#transactions)

## Known app-IDs/facets

Every website using U2F has an app-ID. For example, the app-ID of [Yubico's U2F demo page](https://demo.yubico.com/u2f) is `https://demo.yubico.com`. When the low-level U2F authenticator receives a request to register/authenticate a website, it doesn't receive the friendly app-ID string. Instead, it receives a SHA256 digest of the app-ID. To be able to show a helpful alert message when a website is trying to register/authenticate, a list of app-ID digests is maintained in this repository. You can find the list [here](https://github.com/ibigbug/SoftU2F-Win/blob/master/APDU/KnownFacets.cs). If your company's app-ID is missing from this list, open a pull request to add it.

## Licensing

This project is [Unlicensed](https://github.com/ibigbug/SoftU2F-Win/blob/master/LICENSE) yet.

## Credits

Lots of credits to the original work of [SoftU2F](https://github.com/github/SoftU2F) done by Github team.
