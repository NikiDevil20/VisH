# VisH

**VisH** (VISual Interface for Hilbert) is a WPF-based graphical user interface for managing and analyzing Gaussian computational chemistry calculations.

The application simplifies the preparation and submission of quantum-chemical calculations to an HPC cluster and provides tools for managing calculation results and visualizing molecular structures.

## Features

* **Cluster Management:** Connect to the HPC cluster via SSH, submit Gaussian calculations, and monitor job status.
* **Job Configuration:** Create molecular geometries from SMILES strings and configure calculation parameters such as functional, basis set, charge, and electronic state.
* **Automated Calculation Workflows:** Absorption and emission calculations automatically start the required geometry optimization first. Once the optimization finishes successfully, the subsequent calculation is submitted automatically using PBS `afterok` dependencies.
* **Results Management:** Download and organize calculation results from the cluster.
* **Gaussian Log File Analysis:** Parse Gaussian log files to extract calculation results.
* **SMILES Support:** Validate and visualize chemical structures using RDKit.
* **Multiple Electronic States:** Configure calculations for different singlet and triplet states.

> **Important:** When an absorption or emission calculation is submitted, VisH automatically starts a geometry optimization first. The subsequent calculation is only submitted after the optimization job has completed successfully according to the PBS `afterok` dependency.
>
> However, VisH currently **does not check the number of imaginary frequencies (`NImag`)** after the geometry optimization. A calculation may therefore proceed even if the optimized structure is actually a saddle point rather than a true minimum. Always check `NImag` in the Gaussian output before interpreting the subsequent calculation.

## Installation

VisH is distributed as a Windows installer through the project's GitHub Releases.

1. Open the **Releases** section of the repository.
2. Download the latest VisH installer.
3. Run the installer and follow the installation instructions.
4. Start VisH after installation. When launching for the first time, a setup window opens.
5. Choose Folders and location of the SSH key (see below).
6. Restart the application.

Python and the required Python packages are bundled with the application, so **no separate Python installation or Python environment setup is required**.

## SSH Key Setup

VisH uses SSH key authentication to connect to the HPC cluster. The public key of the computer running VisH must be added to your cluster account.

The storage server used for SSH access is:

```text
username@storage.hpc.rz.uni-duesseldorf.de
```

### 1. Check whether an SSH key already exists

Open [PowerShell](https://docs.microsoft.com/en-us/powershell/) and check your `.ssh` directory:

```powershell
Get-ChildItem $env:USERPROFILE\.ssh
```

If you already have an Ed25519 key pair, you should see files such as:

```text
id_ed25519
id_ed25519.pub
```

If no suitable key exists, create one with:

```powershell
ssh-keygen -t ed25519
```

Press Enter to accept the default file location. You may optionally protect the key with a passphrase.

### 2. Copy the public key to the cluster

If password-based SSH login is available, the public key can be copied to the cluster with:

```powershell
Get-Content $env:USERPROFILE\.ssh\id_ed25519.pub | ssh username@storage.hpc.rz.uni-duesseldorf.de "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"
```

Replace `username` with your cluster username.

You will be prompted for your cluster password. The command adds the public key to:

```text
~/.ssh/authorized_keys
```

> **Important:** Only copy the `.pub` file to the cluster. Never share or upload your private key (`id_ed25519`).

### 3. Test the SSH connection

After adding the key, test the connection:

```powershell
ssh username@storage.hpc.rz.uni-duesseldorf.de
```

If the key is configured correctly, SSH should authenticate without asking for your cluster password.

### 4. Configure the SSH key in VisH

Set the path to your private key in `config.json`:

```json
"SshKeyPath": "C:\\Users\\your_username\\.ssh\\id_ed25519"
```

The private key remains on your local computer and is used by VisH for SSH authentication.

## Calculation Workflows

VisH supports different types of Gaussian calculations.

For **absorption** and **emission** calculations, the workflow consists of two consecutive jobs:

1. VisH submits a geometry optimization.
2. The absorption or emission calculation is submitted with a PBS `afterok` dependency.
3. PBS starts the second calculation only if the geometry optimization job terminates successfully.

This prevents the second calculation from starting if the optimization job itself fails.

### Important: Check for Imaginary Frequencies

A successfully completed geometry optimization does **not necessarily mean that a true minimum was found**.

VisH currently does not automatically evaluate `NImag` before starting the dependent calculation. Therefore, after the optimization, check the Gaussian output for imaginary frequencies.

For a true minimum:

```text
NImag = 0
```

If imaginary frequencies are present, the structure may correspond to a **saddle point** rather than a minimum. The subsequent absorption or emission calculation should then be evaluated with this in mind.

## Project Structure

```text
VisH/
├── View/
├── ViewModel/
├── Model/
├── Data/
├── Assets/
└── PythonScripts/
```

* `View/`: XAML views and user controls.
* `ViewModel/`: MVVM ViewModels.
* `Model/`: Data structures and application logic.
* `Data/`: Configuration files such as `config.json`.
* `Assets/`: Images and other static resources.
* `PythonScripts/`: Python scripts used by VisH for molecular structure generation and Gaussian log file analysis.

## Python Scripts

VisH uses several Python scripts internally:

* `CoordBuilder.py`: Generates 3D coordinates from SMILES.
* `ParseLogfile.py`: Extracts information from Gaussian `.log` files using `cclib`.
* `DrawPngFromSmiles.py`: Generates PNG images of molecular structures.
* `DrawSvgFromSmiles.py`: Generates SVG images of molecular structures.
* `SmilesVal
