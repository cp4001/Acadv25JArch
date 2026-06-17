space : dialog {
  label =  "Pipe Spacing   By g.h.kim";
    : row {
        : column {
            label = "     << Main Pipe >>";
            :boxed_radio_row{
                label = "Main Type";
                : radio_button {
                    label = "Flange";
                    key   = "zmflg";
                    value = "1";
                    mnemonic = "F";
                }
                : radio_button {
                    label = "Pipe";
                    key   = "zmpip";
                    value = "2";
                    mnemonic = "P";
                }
            }
            : popup_list {
                label = "Rating";
                key = "mrat";
                edit_width = 6;
            }
            : popup_list {
                label = "Insul TH'K";
                key = "mthk";
                edit_width = 6;
            }
            : list_box {
                label = "Size";
                key = "mainp";
                width = 16;
                allow_accept = true;
            }
            : concatenation {
                : text_part {
                    label = "Flange Dia :  ";
                }
                : text_part {
                    key = "zm-dia";
                    width = 5;
                }
            }
            : concatenation {
                : text_part {
                    label = "Space Dist1: ";
                }
                : edit_box {
                    key = "zsdist1";
                    width = 9;
                }
            }
        }
        : column {
            label = "     << Sub Pipe >>";
            :boxed_radio_row{
                label = "Sub Type";
                : radio_button {
                    label = "Flange";
                    key   = "zsflg";
                    value = "1";
                    mnemonic = "F";
                }
                : radio_button {
                    label = "Pipe";
                    key   = "zspip";
                    value = "2";
                    mnemonic = "P";
                }
            }
            : popup_list {
                label = "Rating";
                key = "srat";
                edit_width = 6;
            }
            : popup_list {
                label = "Insul TH'K";
                key = "sthk";
                edit_width = 6;
            }
            : list_box {
                    label = "Size ";
                    key = "subp";
                    width = 16;
                    allow_accept = true;
            }
            : concatenation {
                : text_part {
                    label = "Pipe Dia   :  ";
                }
                : text_part {
                    key = "zs-dia";
                    width = 5;
                }
            }
            : concatenation {
                : text_part {
                    label = "Space Dist : ";
                }
                : edit_box {
                    key = "zsdist";
                    width = 9;
                }
            }
        }
    }
  ok_cancel;
}
