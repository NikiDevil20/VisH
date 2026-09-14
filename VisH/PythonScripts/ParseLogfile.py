from pathlib import Path
import json
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

def parse_logfile(logfile_path):
    json_dict = {}
    nhomo = 0
    freqs = []
    mo_energies = []
    scf_energies = []
    coord_block = []

    in_orientation = False
    skip_orientation_headers = 0
    current_orientation = []

    with open(logfile_path, "r", encoding="utf-8", errors="ignore") as logfile:
        for raw_line in logfile:
            line = raw_line.rstrip("\n")

            nhomo_match = re.search(r"N\s*Imag\s*=\s*(\d+)", line, re.IGNORECASE)
            if nhomo_match:
                nhomo = int(nhomo_match.group(1))

            freq_match = re.search(r"Frequencies --\s+([-\d.\s]+)", line)
            if freq_match:
                freqs.extend(float(value) for value in freq_match.group(1).split())

            scf_match = re.search(r"SCF Done:\s+E\([^)]+\)\s+=\s+(-?\d+\.\d+)", line)
            if scf_match:
                scf_energies.append(float(scf_match.group(1)))

            mo_match = re.search(r"Alpha\s+occ\.\s+eigenvalues\s+--\s+([-\d.\s]+)", line)
            if mo_match:
                mo_energies.extend(float(value) for value in mo_match.group(1).split())

            if "Standard orientation:" in line:
                in_orientation = True
                skip_orientation_headers = 5
                current_orientation = []
                continue

            if in_orientation:
                if skip_orientation_headers > 0:
                    skip_orientation_headers -= 1
                    continue

                if re.match(r"\s*-+\s*$", line):
                    if current_orientation:
                        coord_block = current_orientation
                    in_orientation = False
                    continue

                parts = line.split()
                if len(parts) >= 6:
                    atomnumber = int(parts[1])
                    atomsymbol = ATOMIC_SYMBOLS.get(atomnumber, "X")
                    x, y, z = parts[3], parts[4], parts[5]
                    current_orientation.append(f"{atomsymbol} {x} {y} {z}")

    json_dict["NHomo"] = nhomo
    json_dict["AllFreqs"] = freqs
    json_dict["MoEnergies"] = mo_energies
    json_dict["ScfEnergies"] = scf_energies
    json_dict["CoordResults"] = "\n".join(coord_block)
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
