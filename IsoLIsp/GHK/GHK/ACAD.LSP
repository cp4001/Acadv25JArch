(VMON)
; Modified by Choi Y.E. 1989.7.4.
; ACAD.LSP v10.41 Programmed by GID (Goh, Il-Du) 
; Latest Revision 12:14am May 11,1989
; Clean, Load AutoLISP Function Resident or Transient
; Use RLOAD, FLOAD, ALOAD, CLEAN, CLEANALL in command: prompt
; RLOAD loads functions resident, FLOAD or CLEAN can't clear these functions
; FLOAD clears function FLOADed prevoiusly, & Loads function transient
; ALOAD appends functions to transient area
; xLOAD searchs .LSP file in normal ACAD search directory first, C:\LSP\ next
; xLOAD executes function automatically which is named "c:filename" if exist
; CLEAN clears transient functions, & Closes un-closed files automatically
; CLEANALL clears all transient & resident functions and un-closed files also
; !!!
; !!!   W A R N N I N G  :  Do Not Use (VMON)  :  Use 'EXTENDED LISP' Instead
; !!!


(VMON)
(defun dtr (a)
	(* pi (/ a 180.0))
)

(defun rtd (a)
	(/ (* a 180.0) pi)
)
;-------------------------ACAD.LSP Original Copy------------------------------
(if (null @fname) 
    (progn
       (setq @fname "")
       (princ "\n \n \nLoading ACAD.LSP...  ")
    )
)
(setq @errfn *error*
      @oitem "" 
      @loads ""
      @resid ""
      @clean ""
      @cmark ""
      @exec ""
      @ffile ""
      @msg ""
      @cln ""
      @wkp ""
      @wkf ""
      @wk ""
      c:fload "" 
      c:aload ""
      c:rload ""
      c:cleanall ""
      c:clean ""
)
(defun @ffile (@wkf / @wk)
  (if (null (setq @wk (findfile (strcat @wkf ".lsp"))))
      (setq @wk (findfile (strcat "/acad/lisp/" @wkf ".lsp")))
  )
  @wk
)
(defun @loads (@msg @cln @wkp / @wk)
  (setq @resid @wkp)
  (setq @wk (getstring (strcat (if @cln "Clean & " "") @msg " Function" "<" @fname ">: " )))
  (if (/= "" @wk)(setq @fname @wk))
  (if (setq @wk (@ffile @fname))
      (progn
	  (if @cln (@clean '@@CMRK))
	  (if (and @resid (= (car atomlist) '@@CMRK))
	      (setq atomlist (cdr atomlist))
	  )
	  (load @wk)
      )
      (progn
	  (princ "Function not found")
	  (prin1)
      )
  )
)
(defun @exec (/ @wk)
  (setq @wk (read (strcat "c:" @fname )))
  (if (eval @wk)
      (eval (list @wk))
      (prin1)
  )
)
(defun C:fload ()
  (@loads "Load" T nil)
  (@exec)
)
(defun C:aload ()
  (@loads "Append" nil nil)
  (@exec)
)
(defun C:rload ()
  (@loads "Reside" T T)
  (load "acad")
  (@exec)
)
(LOAD"GHK")
(LOAD"ONEKEY")
