;;-----------------------------------------------------------------------------
;;      DIM_EDIT.LSP
;;
;;                      program : Park  heung sik
;;                      d a t e : 1994. 5. 16 mon
;;
;;-----------------------------------------------------------------------------
(prompt "\nLoading DIM_EDIT.lsp ......\n")

(DEFUN dim_edit( / old_error dum dcl_file CHANGE_DIMENSION_TEXT MAIN)
        (setvar "cmdecho" 0)
        (setq old_error *error*
              *error* PHS_ERR
              dcl_file "dim_edit.dcl"
        )

        (cond   ((null (findfile dcl_file))
                        (princ  (strcat "\n "dcl_file" not found !! \n"))
                )
                (T (while (setq dum (ssget))
                        (progn
                           (setq id 0)
                           (repeat (sslength dum)
                                (DIM_EDIT_INIT)
                                (MAIN (ssname dum id))
                                (setq id (+ 1 id))
                            )
                        )
                   )
                )
        )
        (setq *error* old_error)
        (setvar "cmdecho" 1)
        (princ)
)

;;
;; Initialize program
;;

(DEFUN DIM_EDIT_INIT()
;;
;; Main program
;;
        (DEFUN MAIN( dum / dum dum1 dum2 dum3 txt1)
                (setq dum1 (cdr (assoc 0 (entget dum))))
                (cond   ((= dum1 "DIMENSION")
                                (setq dum1 (entget dum))
                                (if (= (cdr (assoc 1 (entget dum))) "")
                                        (if (= (caddr (assoc 10 dum1)) (caddr (assoc 14 dum1)))
                                                (setq pt1 (list 0 (caddr (assoc 13 dum1)))
                                                      pt2 (list 0 (caddr (assoc 14 dum1)))
                                                      txt1 (rtos (distance pt1 pt2))
                                                )
                                                (if (= (cadr (assoc 10 dum1)) (cadr (assoc 14 dum1)))
                                                        (setq pt1 (list (cadr (assoc 13 dum1)) 0)
                                                              pt2 (list (cadr (assoc 14 dum1)) 0)
                                                              txt1 (rtos (distance pt1 pt2))
                                                        )
                                                        (setq pt1 (cdr (assoc 13 dum1))
                                                              pt2 (cdr (assoc 14 dum1))
                                                              txt1 (rtos (distance pt1 pt2))
                                                        )
                                                )
                                        )
                                        (setq txt1 (cdr (assoc 1 (entget dum))))
                                )
                                (setq txt1 (CHANGE_DIMENSION_TEXT txt1))
                                (if (/= txt1 NIL)
                                        (progn
                                                (setq dum1 (subst (cons 1 txt1) (assoc 1 dum1) dum1))
                                                (entmod dum1)
                                        )
                                )
                        )
                        ((or (eq dum1 "TEXT") (eq dum1 "MTEXT")  (eq dum1 "ATTDEF"))
                                (command ".ddedit" dum "")
                        )
                        ((= dum1 "INSERT")
                                (command ".DDATTE" dum "")
                        )
                        (T (princ (strcat "\n "dum1" is invalid object. ")))
                )
        )
;;
;; Change dimension text
;;
        (DEFUN CHANGE_DIMENSION_TEXT(dum / what_next dum1 text dcl_id)
                (setq dcl_id (load_dialog dcl_file))
                (setq what_next 2)
                (while (> what_next 1)
                        (if (not (new_dialog "dim_edit" dcl_id))
                                (exit)
                                (progn
                                        (set_tile "dum1" dum)
                                        (mode_tile "dum1" 2)
                                        (action_tile "dum1" "(setq text $value)")
                                        (setq what_next (start_dialog))
                                )
                        )
                )
                (done_dialog)
                (unload_dialog dcl_id)
                (if (= what_next 1)
                        (setq dum text)
                        (setq dum NIL)
                )
        )
)

;;
;; Error message
;;
(DEFUN PHS_ERR(a)
        (if (not (member a '("Function cancelled" "console break")))
                (princ (strcat "\nP_Error : " a))
        )
        (if old_error (setq *error* old_error))
        (princ)
)

;; prompt dim_edit.lsp
(princ "\n\t Loading DIM_EDIT.LSP ...   You can used command DED \n ")
(princ)
