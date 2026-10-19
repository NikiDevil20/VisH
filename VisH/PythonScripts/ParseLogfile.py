from pathlib import Path
import json
import math
import re
import sys
import traceback

ATOMIC_SYMBOLS = {
    1: "H", 2: "He", 3: "Li", 4: "Be", 5: "B", 6: "C", 7: "N", 8: "O", 9: "F", 10: "Ne",
    11: "Na", 12: "Mg", 13: "Al", 14: "Si", 15: "P", 16: "S", 17: "Cl", 18: "Ar",
    19: "K", 20: "Ca", 21: "Sc", 22: "Ti", 23: "V", 24: "Cr", 25: "Mn", 26: "Fe",
    27: "Co", 28: "Ni", 29: "Cu", 30: "Zn", 31: "Ga", 32: "Ge", 33: "As", 34: "Se",
    35: "Br", 36: "Kr", 37: "Rb", 38: "Sr", 39: "Y", 40: "Zr", 41: "Nb", 42: "Mo",
    43: "Tc", 44: "Ru", 45: "Rh", 46: "Pd", 47: "Ag", 48: "Cd", 49: "In", 50: "Sn",
    51: "Sb", 52: "Te", 53: "I", 54: "Xe", 55: "Cs", 56: "Ba", 57: "La", 58: "Ce",
    59: "Pr", 60: "Nd", 61: "Pm", 62: "Sm", 63: "Eu", 64: "Gd", 65: "Tb", 66: "Dy",
    67: "Ho", 68: "Er", 69: "Tm", 70: "Yb", 71: "Lu", 72: "Hf", 73: "Ta", 74: "W",
    75: "Re", 76: "Os", 77: "Ir", 78: "Pt", 79: "Au", 80: "Hg", 81: "Tl", 82: "Pb",
    83: "Bi", 84: "Po", 85: "At", 86: "Rn", 87: "Fr", 88: "Ra", 89: "Ac", 90: "Th",
    91: "Pa", 92: "U", 93: "Np", 94: "Pu", 95: "Am", 96: "Cm", 97: "Bk", 98: "Cf",
    99: "Es", 100: "Fm", 101: "Md", 102: "No", 103: "Lr", 104: "Rf", 105: "Db", 106: "Sg",
    107: "Bh", 108: "Hs", 109: "Mt", 110: "Ds", 111: "Rg", 112: "Cn", 113: "Nh", 114: "Fl",
    115: "Mc", 116: "Lv", 117: "Ts", 118: "Og"
}

def find_closed_shell_orbitals(occupied_orbitals, virtual_orbitals):
    if not occupied_orbitals or not virtual_orbitals:
        return None, None
    return len(occupied_orbitals) - 1, len(occupied_orbitals)

def parse_orbital_values(line, spin, orbital_kind):
    match = re.search(
        rf"^{spin}\s+{orbital_kind}\s+eigenvalues\s+--\s+(.+)$",
        line,
        re.IGNORECASE,
    )
    if not match:
        return None

    values = []
    for value in match.group(1).split():
        try:
            values.append(float(value.replace("D", "E").replace("d", "e")))
        except ValueError:
            return None
    return values if values else None

def select_last_complete_restricted_orbitals(orbital_groups):
    complete_groups = [
        group for group in orbital_groups
        if group["occupied"] and group["virtual"]
    ]
    if not complete_groups:
        return None
    return complete_groups[-1]

def parse_last_complete_standard_orientation(lines):
    valid_blocks = []
    marker = "Standard orientation:"
    for index, line in enumerate(lines):
        if marker.lower() not in line.lower():
            continue

        cursor = index + 1
        while cursor < len(lines) and not re.match(r"\s*-{5,}\s*$", lines[cursor]):
            cursor += 1
        if cursor >= len(lines) or cursor + 3 >= len(lines):
            continue

        data_start = cursor + 4
        if not re.match(r"\s*-{5,}\s*$", lines[data_start - 1]):
            continue

        atoms = []
        cursor = data_start
        complete = False
        while cursor < len(lines):
            line = lines[cursor]
            if re.match(r"\s*-{5,}\s*$", line):
                complete = bool(atoms)
                break

            parts = line.split()
            if len(parts) != 6:
                complete = False
                break

            try:
                atomic_number = int(parts[1])
                x, y, z = (float(value.replace("D", "E").replace("d", "e"))
                           for value in parts[3:6])
            except ValueError:
                complete = False
                break

            if (atomic_number not in ATOMIC_SYMBOLS or
                    not all(math.isfinite(value) for value in (x, y, z))):
                complete = False
                break

            atoms.append(
                f"{ATOMIC_SYMBOLS[atomic_number]} {parts[3]} {parts[4]} {parts[5]}"
            )
            cursor += 1

        if complete:
            valid_blocks.append(atoms)

    if not valid_blocks:
        return [], False, "Keine vollständige gültige Standard-orientation-Tabelle gefunden."
    return valid_blocks[-1], True, ""

def parse_logfile(logfile_path):
    json_dict = {}
    orbital_groups = []
    current_orbital_group = None
    unrestricted = False
    freqs = []
    scf_energies = []
    coord_block = []
    geometry_valid = False
    geometry_message = ""
    explicit_nimag = None
    normal_termination = None
    optimization_converged = None
    termination_events = []
    optimization_events = []

    with open(logfile_path, "r", encoding="utf-8", errors="ignore") as logfile:
        lines = logfile.readlines()

    coord_block, geometry_valid, geometry_message = parse_last_complete_standard_orientation(lines)

    for raw_line in lines:
        line = raw_line.rstrip("\n")

        if re.search(r"Normal termination", line, re.IGNORECASE):
            termination_events.append("normal")

        if re.search(r"Error termination", line, re.IGNORECASE):
            termination_events.append("error")

        if re.search(r"Optimization completed", line, re.IGNORECASE):
            optimization_events.append(True)

        if re.search(r"Optimization stopped", line, re.IGNORECASE):
            optimization_events.append(False)

        nimag_match = re.search(r"N\s*Imag\s*=\s*(\d+)", line, re.IGNORECASE)
        if nimag_match:
            explicit_nimag = int(nimag_match.group(1))

        freq_match = re.search(r"Frequencies --\s+([-\d.\s]+)", line)
        if freq_match:
            freqs.extend(float(value) for value in freq_match.group(1).split())

        scf_match = re.search(r"SCF Done:\s+E\([^)]+\)\s+=\s+(-?\d+\.\d+)", line)
        if scf_match:
            scf_energies.append(float(scf_match.group(1)))

        for spin in ("Alpha", "Beta"):
            for orbital_kind in ("occ.", "virt."):
                values = parse_orbital_values(line.strip(), spin, orbital_kind)
                if values is None:
                    continue

                if spin.lower() == "beta":
                    unrestricted = True
                    current_orbital_group = None
                    continue

                if orbital_kind == "occ.":
                    if current_orbital_group is not None:
                        orbital_groups.append(current_orbital_group)
                    current_orbital_group = {"occupied": [], "virtual": []}
                    current_orbital_group["occupied"].extend(values)
                elif current_orbital_group is not None:
                    current_orbital_group["virtual"].extend(values)

    if current_orbital_group is not None:
        orbital_groups.append(current_orbital_group)

    orbital_group = None if unrestricted else select_last_complete_restricted_orbitals(orbital_groups)
    if orbital_group is None:
        json_dict["NHomo"] = None
        json_dict["NLumo"] = None
        json_dict["MoEnergies"] = []
        json_dict["OrbitalDataAvailable"] = False
    else:
        homo_index, lumo_index = find_closed_shell_orbitals(
            orbital_group["occupied"],
            orbital_group["virtual"],
        )
        json_dict["NHomo"] = homo_index
        json_dict["NLumo"] = lumo_index
        json_dict["MoEnergies"] = orbital_group["occupied"] + orbital_group["virtual"]
        json_dict["OrbitalDataAvailable"] = True

    if explicit_nimag is not None:
        json_dict["NImag"] = explicit_nimag
    elif freqs:
        json_dict["NImag"] = sum(1 for frequency in freqs if frequency < 0)
    else:
        json_dict["NImag"] = None

    json_dict["NImagFromFrequencies"] = (
        sum(1 for frequency in freqs if frequency < 0)
        if freqs else None
    )
    if termination_events:
        normal_termination = termination_events[-1] == "normal"
    if optimization_events:
        optimization_converged = optimization_events[-1]

    json_dict["NormalTermination"] = normal_termination
    json_dict["OptimizationConverged"] = optimization_converged
    json_dict["Unrestricted"] = unrestricted
    json_dict["AllFreqs"] = freqs
    json_dict["ScfEnergies"] = scf_energies
    json_dict["CoordResults"] = "\n".join(coord_block)
    json_dict["GeometryValid"] = geometry_valid
    json_dict["GeometryMessage"] = geometry_message
    json_dict["Version"] = 1

    return json_dict

if __name__ == "__main__":
    logfile = sys.argv[1]
    directory = Path(sys.argv[2])
    log_path = directory / "parse_logfile_debug.txt"

    try:
        with open(log_path, "a", encoding="utf-8") as debug:
            debug.write(f"logfile={logfile}\n")
            debug.write(f"directory={directory}\n")
            debug.write("starting parse\n")

        json_dict = parse_logfile(logfile)

        result_path = directory / "result.json"
        with open(result_path, "w", encoding="utf-8") as f:
            json.dump(json_dict, f, indent=2)
            f.flush()

        with open(log_path, "a", encoding="utf-8") as debug:
            debug.write(f"result.json written to {result_path}\n")
            debug.write(f"exists={result_path.exists()}\n")
    except Exception:
        with open(log_path, "a", encoding="utf-8") as debug:
            debug.write(traceback.format_exc())
        raise
