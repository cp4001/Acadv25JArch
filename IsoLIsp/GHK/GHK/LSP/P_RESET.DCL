p_reset : dialog {
label =  "Kic Menu Config...";
    : column {
        :row{
            :boxed_radio_row{
                label = "Appl. code ";
                : radio_button {
                   label = "ANSI(API)";
                   key   = "ansicode";
                   value = "1";
                   mnemonic = "A";
                }
                : radio_button {
                   label = "KS(JIS)";
                   key   = "kscode";
                   value = "2";
                   mnemonic = "K";
                }
            }
            :boxed_radio_row{
                label = "Flange Type";
                : radio_button {
                   label = "Single";
                   key   = "s-flg";
                   value = "1";
                   mnemonic = "S";
                }
                : radio_button {
                   label = "Double";
                   key   = "d-flg";
                   value = "2";
                   mnemonic = "K";
                }
            }
        }
        : row {
            : concatenation {
                : text_part {
                    label = "Gasket TH'KW";
                }
                : edit_box {
                    key = "zgasket";
                    width = 9;
                }
            }
            : concatenation {
                : text_part {
                    label = "Weld Clean";
                }
                : edit_box {
                    key = "zweldc";
                    width = 9;
                }
            }
        }
        : row {
            : boxed_column {
                label = "Weld Size";
                : concatenation {
                    : text_part {
                        label = "Weld - 1 : ";
                    }
                    : edit_box {
                        key = "zweld1";
                        width = 9;
                    }
                }
                : concatenation {
                    : text_part {
                        label = "Weld - 2 : ";
                    }
                    : edit_box {
                        key = "zweld2";
                        width = 9;
                    }
                }
                : concatenation {
                    : text_part {
                        label = "Weld - 3 : ";
                    }
                    : edit_box {
                        key = "zweld3";
                        width = 9;
                    }
                }
                : button {
                    label = "Defaults.";
                    key = "defu";
                    mnemonic = "D";
                    fixed_width = true;
                }
            }
            : boxed_column {
                label = "End Conn. Size";
                : concatenation {
                    : text_part {
                        label = "Sock     : ";
                    }
                    : edit_box {
                        key = "zsock";
                        width = 9;
                    }
                }
                : concatenation {
                    : text_part {
                        label = "Large    : ";
                    }
                    : edit_box {
                        key = "zlarge";
                        width = 9;
                    }
                }
                : concatenation {
                    : text_part {
                        label = "Double   : ";
                    }
                    : edit_box {
                        key = "zdouble";
                        width = 9;
                    }
                }
            }
        }
    }
ok_cancel;
}
