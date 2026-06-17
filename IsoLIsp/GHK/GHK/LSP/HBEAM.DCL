hbeam : dialog {
  label =  "Autocad H-Beam";
    : row {
        : list_box {
                    label = "H-beam size    ";
                    key = "hs";
                    width = 10;
                    allow_accept = true;
        }
        : list_box {
                    label = "Thickless";
                    key = "thk";
                    width = 10;
                    allow_accept = true;
        }
        : boxed_column {
            :boxed_radio_row{
                label = "Line Type";
                : radio_button {
                    label = "P-line";
                    key   = "zpline";
                    value = "1";
                    mnemonic = "P";
                }
                : radio_button {
                    label = "Line";
                    key   = "zline";
                    value = "2";
                    mnemonic = "L";
                }
            }
            label = "----- Dscription ------";
            : concatenation {
                : text_part {
                    label = "Size(hs-)   :";
                }
                : text_part {
                    key = "zhs";
                    width = 9;
                }
            }
            : concatenation {
                : text_part {
                    label = "H (hh)      :";
                }
                :edit_box  {
                    key = "zhh";
                    width = 5;
                }
            }
            : concatenation {
                : text_part {
                    label = "B (hb)      :";
                }
                :edit_box  {
                    key = "zhb";
                    width = 5;
                }
            }
            : concatenation {
                : text_part {
                    label = "Th'k1 ( t1 ):";
                }
                :edit_box  {
                    key = "zt1";
                    width = 5;
                }
            }
            : concatenation {
                : text_part {
                    label = "Th'k2 ( t2 ):";
                }
                :edit_box  {
                    key = "zt2";
                    width = 5;
                }
            }
            : concatenation {
                : text_part {
                    label = "Round( hr1 ):";
                }
                :edit_box  {
                    key = "zhr1";
                    width = 5;
                }
            }
            : concatenation {
                : text_part {
                    label = "Round( hr2 ):";
                }
                :edit_box  {
                    key = "zhr2";
                    width = 5;
                }
            }
        }
    }
  ok_cancel;
}
