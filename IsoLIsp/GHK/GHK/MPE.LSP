(defun GETDIR (PATH / RES FH)
    (command "SHELL" (strcat "DIR/on " PATH " > DATA.DIR"))
    (setq FH (open "DATA.DIR" "r"))
    (while (setq LN (read-line FH))
       (if (and (> (strlen LN) 0) (/= (substr LN 1 1) " "))
           (setq RES (cons (substr LN 1 8) RES)))
    );;while
    (close FH)
    (mapcar '(lambda (X)
       (while (= (substr X (strlen X) 1) " ")
              (setq X (substr X 1 (1- (strlen X))))
       );;while
       (setq X X))
    (reverse RES))
);;defun

;;------------------------------------------------------------------

(defun PLOT (DWG FH)
    (write-line "FILEDIA 0 ")
    (write-line "OPEN" FH)
    (write-line DWG FH)
    (write-line "ZOOM A PLOT L 0" FH)
    (write-line "qsave" fh)
);;defun

;;------------------------------------------------------------------

(defun C:MPE (/ ALIST FH ITEM)
    (prompt "made by Chang-yeol, YEOM.")
    (setq ALIST (getdir "*.dwg")
          FH (open "PLOT.SCR" "w"))
    (foreach ITEM ALIST
       (PLOT ITEM FH))
       (setq FH (close FH))
    (prompt "\nAutomatic Script file built, passing control...")
    (command "SCRIPT" "PLOT")
)

;;********************END OF PROGRAM********************************
