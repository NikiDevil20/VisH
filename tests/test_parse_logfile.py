import importlib.util
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).parents[1] / "VisH" / "PythonScripts" / "ParseLogfile.py"
SPEC = importlib.util.spec_from_file_location("parse_logfile", SCRIPT_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


def parse_text(text):
    with tempfile.TemporaryDirectory() as directory:
        path = Path(directory) / "calculation.log"
        path.write_text(text, encoding="utf-8")
        return MODULE.parse_logfile(path)


class ParseLogfileTests(unittest.TestCase):
    def test_last_complete_standard_orientation_is_used(self):
        result = parse_text(
            """
 Standard orientation:
 ---------------------------------------------------------------------
 Center     Atomic      Atomic             Coordinates (Angstroms)
 Number     Number       Type             X           Y           Z
 ---------------------------------------------------------------------
      1          6           0        0.000000    0.000000    0.000000
      2          1           0        0.000000    0.000000    1.000000
 ---------------------------------------------------------------------
 Standard orientation:
 ---------------------------------------------------------------------
 Center     Atomic      Atomic             Coordinates (Angstroms)
 Number     Number       Type             X           Y           Z
 ---------------------------------------------------------------------
      1          8           0        0.100000    0.000000    0.000000
 ---------------------------------------------------------------------
"""
        )

        self.assertTrue(result["GeometryValid"])
        self.assertEqual(result["CoordResults"], "O 0.100000 0.000000 0.000000")

    def test_incomplete_orientation_is_not_used(self):
        result = parse_text(
            """
 Standard orientation:
 ---------------------------------------------------------------------
 Center     Atomic      Atomic             Coordinates (Angstroms)
 Number     Number       Type             X           Y           Z
 ---------------------------------------------------------------------
      1          6           0        0.000000    0.000000    0.000000
"""
        )

        self.assertFalse(result["GeometryValid"])
        self.assertEqual(result["CoordResults"], "")

    def test_invalid_atomic_number_makes_geometry_unavailable(self):
        result = parse_text(
            """
 Standard orientation:
 ---------------------------------------------------------------------
 Center     Atomic      Atomic             Coordinates (Angstroms)
 Number     Number       Type             X           Y           Z
 ---------------------------------------------------------------------
      1        999           0        0.000000    0.000000    0.000000
 ---------------------------------------------------------------------
"""
        )

        self.assertFalse(result["GeometryValid"])
        self.assertEqual(result["CoordResults"], "")

    def test_last_complete_orbital_dataset_is_selected(self):
        result = parse_text(
            """
 SCF Done:  E(RB3LYP) =  -100.000
 Alpha  occ. eigenvalues -- -0.60 -0.30
 Alpha virt. eigenvalues --  0.10  0.40
 SCF Done:  E(RB3LYP) =  -100.100
 Alpha  occ. eigenvalues -- -0.70 -0.35
 Alpha virt. eigenvalues --  0.05  0.25
 Normal termination of Gaussian 16
"""
        )

        self.assertEqual(result["MoEnergies"], [-0.70, -0.35, 0.05, 0.25])
        self.assertEqual(result["NHomo"], 1)
        self.assertEqual(result["NLumo"], 2)
        self.assertTrue(result["OrbitalDataAvailable"])

    def test_multiple_optimization_steps_do_not_accumulate_orbitals(self):
        result = parse_text(
            """
 Step 1
 Alpha  occ. eigenvalues -- -0.50 -0.20
 Alpha virt. eigenvalues --  0.15  0.35
 Optimization completed.
 Step 2
 Alpha  occ. eigenvalues -- -0.55 -0.25
 Alpha virt. eigenvalues --  0.12  0.30
 Optimization completed.
 Normal termination of Gaussian 16
"""
        )

        self.assertEqual(result["MoEnergies"], [-0.55, -0.25, 0.12, 0.30])
        self.assertEqual(result["NHomo"], 1)
        self.assertEqual(result["NLumo"], 2)

    def test_incomplete_last_orbital_dataset_uses_last_complete_dataset(self):
        result = parse_text(
            """
 Alpha  occ. eigenvalues -- -0.50 -0.20
 Alpha virt. eigenvalues --  0.10  0.30
 Alpha  occ. eigenvalues -- -0.80 -0.40
 Normal termination of Gaussian 16
"""
        )

        self.assertEqual(result["MoEnergies"], [-0.50, -0.20, 0.10, 0.30])
        self.assertTrue(result["OrbitalDataAvailable"])

    def test_missing_orbital_data_is_unavailable(self):
        result = parse_text(
            """
 SCF Done:  E(RB3LYP) = -100.0
 Alpha  occ. eigenvalues -- -0.50 -0.20
 Normal termination of Gaussian 16
"""
        )

        self.assertIsNone(result["NHomo"])
        self.assertIsNone(result["NLumo"])
        self.assertEqual(result["MoEnergies"], [])
        self.assertFalse(result["OrbitalDataAvailable"])

    def test_unrestricted_alpha_and_beta_data_disables_closed_shell_orbitals(self):
        result = parse_text(
            """
 SCF Done:  E(UB3LYP) = -100.0
 Alpha  occ. eigenvalues -- -0.50 -0.20
 Alpha virt. eigenvalues --  0.10  0.30
 Beta  occ. eigenvalues -- -0.48 -0.18
 Beta virt. eigenvalues --  0.12  0.32
 Normal termination of Gaussian 16
"""
        )

        self.assertTrue(result["Unrestricted"])
        self.assertFalse(result["OrbitalDataAvailable"])
        self.assertIsNone(result["NHomo"])
        self.assertEqual(result["MoEnergies"], [])

    def test_explicit_nimag_is_available_without_frequency_blocks(self):
        zero = parse_text(
            """
 N Imag = 0
 Normal termination of Gaussian 16
"""
        )
        one = parse_text(
            """
 N Imag = 1
 Normal termination of Gaussian 16
"""
        )

        self.assertEqual(zero["NImag"], 0)
        self.assertEqual(one["NImag"], 1)
        self.assertIsNone(zero["NImagFromFrequencies"])
        self.assertIsNone(one["NImagFromFrequencies"])

    def test_frequency_count_is_independent_plausibility_value(self):
        result = parse_text(
            """
 Frequencies -- -120.0 80.0 100.0
 N Imag = 0
 Normal termination of Gaussian 16
"""
        )

        self.assertEqual(result["NImag"], 0)
        self.assertEqual(result["NImagFromFrequencies"], 1)

    def test_missing_frequency_data_is_not_zero(self):
        result = parse_text("Normal termination of Gaussian 16\n")
        self.assertIsNone(result["NImag"])
        self.assertIsNone(result["NImagFromFrequencies"])

    def test_later_error_termination_overrides_earlier_normal_termination(self):
        result = parse_text(
            """
 Normal termination of Gaussian 16
 Error termination request processed by link 9999.
"""
        )
        self.assertFalse(result["NormalTermination"])

    def test_optimization_status_uses_last_optimization_event(self):
        completed = parse_text(
            """
 Optimization stopped.
 Optimization completed.
 Normal termination of Gaussian 16
"""
        )
        stopped = parse_text(
            """
 Optimization completed.
 Optimization stopped.
 Normal termination of Gaussian 16
"""
        )

        self.assertTrue(completed["OptimizationConverged"])
        self.assertFalse(stopped["OptimizationConverged"])

    def test_normal_optimization_without_frequency_data_is_supported(self):
        result = parse_text(
            """
 Alpha  occ. eigenvalues -- -0.50
 Alpha virt. eigenvalues --  0.10
 Optimization completed.
 Normal termination of Gaussian 16
"""
        )

        self.assertTrue(result["NormalTermination"])
        self.assertTrue(result["OptimizationConverged"])
        self.assertIsNone(result["NImag"])


if __name__ == "__main__":
    unittest.main()
