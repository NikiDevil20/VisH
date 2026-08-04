# GaussianGUI

A WPF-based graphical user interface for managing and analyzing Gaussian computational chemistry calculations. The application allows users to interact with a high-performance computing (HPC) cluster, manage calculation templates, and visualize results.

## Features

- **Cluster Management:** Connect to HPC clusters via SSH for job submission and status monitoring.
- **Job Configuration:** Build molecular coordinates from SMILES strings.
- **Template System:** Manage and use calculation templates for consistent job submission.
- **Results Analysis:** Parse and visualize Gaussian log files (energies, frequencies, orbitals).
- **SMILES Support:** Validate and visualize chemical structures using RDKit.

## Tech Stack

- **Frontend:** WPF (Windows Presentation Foundation)
- **Language:** C# 14.0, .NET 10.0-windows
- **SSH Communication:** [SSH.NET](https://github.com/sshnet/SSH.NET)
- **Scripting:** Python 3 (Integration via `PythonScripts` folder)
- **Python Libraries:** `rdkit`, `cclib`
- **Testing:** xUnit / .NET Test Project

## Requirements

- **Windows OS** (due to WPF)
- **.NET 10.0 SDK**
- **Python 3.x** with the following packages:
  - `rdkit`
  - `cclib`
- **SSH Access** to a cluster (configured in `config.json`)

## Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-repo/GaussianGUI.git
   cd GaussianGUI
   ```

2. **Configure the application:**
   Edit `GaussianTool/Data/config.json` with your cluster details and paths:
   ```json
   {
     "LocalRechnungenPath": "C:\\Path\\To\\Local\\Results",
     "ClusterRechnungenPath": "/home/user/Rechnungen",
     "ClusterUsername": "your_username",
     "Cluster": "hpc.example.edu",
     "Storage": "storage.example.edu",
     "SshKeyPath": "C:\\Users\\user\\.ssh\\id_ed25519"
   }
   ```

3. **Python Environment:**
   It is recommended to set up a virtual environment in `GaussianTool/PythonScripts/venv` and install requirements:
   ```bash
   cd GaussianTool/PythonScripts
   python -m venv venv
   .\venv\Scripts\activate
   pip install rdkit cclib
   ```

## Project Structure

- `GaussianTool/`: Main WPF application project.
  - `View/`: XAML views and user controls.
  - `ViewModel/`: MVVM ViewModels.
  - `Model/`: Data structures and business logic.
  - `Data/`: Configuration files (e.g., `config.json`).
  - `Assets/`: Images and static resources.
  - `PythonScripts/`: Python scripts for SMILES processing and log parsing.
- `GaussianTool.Tests/`: Unit tests for the application.

## Scripts

The application relies on several Python scripts located in `GaussianTool/PythonScripts/`:
- `CoordBuilder.py`: Generates 3D coordinates from SMILES.
- `ParseLogfile.py`: Extracts results from Gaussian `.log` files using `cclib`.
- `DrawPngFromSmiles.py` / `DrawSvgFromSmiles.py`: Generates 2D structure images.
- `SmilesValidation.py`: Validates SMILES strings.

## Running Tests

To run the C# unit tests:
```bash
dotnet test
```

## TODOs / Roadmap

- [ ] Implement asynchronous loading for job status.
- [ ] Add automatic deletion of temporary files after download.
- [ ] Refactor for Dependency Injection.
- [ ] Fix the refresh mechanism in the UI.

## License

[TODO: Add License Information]
