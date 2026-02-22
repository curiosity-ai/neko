---
order: 2
icon: cloud-download
---
# Installation

Neko is a command line tool. Installation is super quick and you can be up and running within seconds.

!!!
Please see the [Neko CLI](cli.md) for full details on each command.
!!!

---

## Step 1: Prerequisites

Neko is installed using the [`dotnet`](https://dotnet.microsoft.com/download/dotnet-core) CLI, which is included with the .NET SDK.

| Package Manager | Supported Platforms |
| --- | --- |
| [`dotnet`](https://dotnet.microsoft.com/download/dotnet-core) | [!badge text="Mac" variant="light"] [!badge text="Win" variant="primary"] [!badge text="Linux" variant="dark"]

---

## Step 2: Install

Once you have the .NET SDK installed, it takes just a few seconds to install Neko.

```
dotnet tool install neko --global
neko start
```

That's it! Your new Neko website should be up and running. :tada:

---

## Other Options

### Update

Update to the latest release of Neko using the following command.

```
dotnet tool update neko --global
```

---

### Uninstall

Done with Neko? It's okay, we understand. :cry:

Uninstalling Neko is just as simple as installing.

```
dotnet tool uninstall neko --global
```

All Neko related files and folders within your project can be deleted, such as the **neko.yml** file and the generated `.neko` folder.

---

### Version Specific

When managing project dependencies, it is sometimes necessary to install a specific version of a software package to ensure compatibility or access to certain features.

In the following sample, replace `[version]` with the actual version number.

```
dotnet tool install neko --global --version [version]
```

---

### Supported Platforms

Neko supports Windows, macOS, and Linux.

#### :icon-package: macOS

{.compact}
OS                                    | Version                 | Architectures     |
--------------------------------------|-------------------------|-------------------|
[macOS][macOS]                        | 10.15+                  | x64, Arm64        |

[macOS]: https://support.apple.com/macos

#### :icon-package: Windows

{.compact}
OS                                    | Version                 | Architectures     |
--------------------------------------|-------------------------|-------------------|
[Windows 10 Client][Windows-client]   | Version 1607+           | x64, x86, Arm64   |
[Windows 11][Windows-client]          | Version 22000+          | x64, x86, Arm64   |
[Windows Server][Windows-Server]      | 2012+                   | x64, x86          |
[Windows Server Core][Windows-Server] | 2012+                   | x64, x86          |
[Nano Server][Nano-Server]            | Version 1809+           | x64               |

[Windows-client]: https://www.microsoft.com/windows/
[Windows-lifecycle]: https://support.microsoft.com/help/13853/windows-lifecycle-fact-sheet
[win-client-docker]: https://hub.docker.com/_/microsoft-windows
[Windows-Server-lifecycle]: https://learn.microsoft.com/windows-server/get-started/windows-server-release-info
[Nano-Server]: https://learn.microsoft.com/windows-server/get-started/getting-started-with-nano-server
[Windows-Server]: https://learn.microsoft.com/windows-server/

#### :icon-package: Linux

{.compact}
OS                                    | Version               | Architectures     |
--------------------------------------|-----------------------|-------------------|
[Alpine Linux][Alpine]                | 3.15+                 | x64, Arm64, Arm32 |
[CentOS Linux][CentOS]                | 7                     | x64               |
[CentOS Stream Linux][CentOS]         | 8                     | x64               |
[Debian][Debian]                      | 10+                   | x64, Arm64, Arm32 |
[Fedora][Fedora]                      | 36+                   | x64               |
[openSUSE][OpenSUSE]                  | 15+                   | x64               |
[Oracle Linux][Oracle-Linux]          | 7+                    | x64               |
[Red Hat Enterprise Linux][RHEL]      | 7+                    | x64, Arm64        |
[SUSE Enterprise Linux (SLES)][SLES]  | 12 SP2+               | x64               |
[Ubuntu][Ubuntu]                      | 18.04+                | x64, Arm64, Arm32 |

[Alpine]: https://alpinelinux.org/
[Alpine-lifecycle]: https://alpinelinux.org/releases/
[CentOS]: https://www.centos.org/
[CentOS-lifecycle]:https://wiki.centos.org/FAQ/General
[CentOS-docker]: https://hub.docker.com/_/centos
[CentOS-pm]: https://learn.microsoft.com/dotnet/core/install/linux-package-manager-centos8
[Debian]: https://www.debian.org/
[Debian-lifecycle]: https://wiki.debian.org/DebianReleases
[Debian-pm]: https://learn.microsoft.com/dotnet/core/install/linux-package-manager-debian10
[Fedora]: https://getfedora.org/
[Fedora-lifecycle]: https://fedoraproject.org/wiki/End_of_life
[Fedora-docker]: https://hub.docker.com/_/fedora
[Fedora-msft-pm]: https://learn.microsoft.com/dotnet/core/install/linux-package-manager-fedora32
[Fedora-pm]: https://fedoraproject.org/wiki/DotNet
[OpenSUSE]: https://opensuse.org/
[OpenSUSE-lifecycle]: https://en.opensuse.org/Lifetime
[OpenSUSE-docker]: https://hub.docker.com/r/opensuse/leap
[OpenSUSE-pm]: https://learn.microsoft.com/dotnet/core/install/linux-package-manager-opensuse15
[Oracle-Linux]: https://www.oracle.com/linux/
[Oracle-Lifecycle]: https://www.oracle.com/a/ocom/docs/elsp-lifetime-069338.pdf
[RHEL]: https://www.redhat.com/en/technologies/linux-platforms/enterprise-linux
[RHEL-lifecycle]: https://access.redhat.com/support/policy/updates/errata/
[RHEL-msft-pm]: https://learn.microsoft.com/dotnet/core/install/linux-package-manager-rhel8
[RHEL-pm]: https://access.redhat.com/documentation/en-us/red_hat_enterprise_linux/8/html/developing_.net_applications_in_rhel_8/using-net-core-on-rhel_gsg#installing-net-core_gsg
[SLES]: https://www.suse.com/products/server/
[SLES-lifecycle]: https://www.suse.com/lifecycle/
[SLES-pm]: https://learn.microsoft.com/dotnet/core/install/linux-package-manager-sles15
[Ubuntu]: https://ubuntu.com/
[Ubuntu-lifecycle]: https://wiki.ubuntu.com/Releases
[Ubuntu-pm]: https://learn.microsoft.com/dotnet/core/install/linux-package-manager-ubuntu-2004
[glibc]: https://www.gnu.org/software/libc/
[musl]: https://musl.libc.org/
