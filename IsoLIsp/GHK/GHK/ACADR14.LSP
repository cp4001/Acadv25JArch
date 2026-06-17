; Next available MSG number is    86
; MODULE_ID ACADR13_LSP_
;;;    ACADR14.LSP Version 14.1 for Release 14 
;;;
;;;    Copyright (C) 1994 - 1997 by Autodesk, Inc.
;;;
;;;    Permission to use, copy, modify, and distribute this software
;;;    for any purpose and without fee is hereby granted, provided
;;;    that the above copyright notice appears in all copies and
;;;    that both that copyright notice and the limited warranty and
;;;    restricted rights notice below appear in all supporting
;;;    documentation.
;;;
;;;    AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
;;;    AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
;;;    MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
;;;    DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
;;;    UNINTERRUPTED OR ERROR FREE.
;;;
;;;    Use, duplication, or disclosure by the U.S. Government is subject to
;;;    restrictions set forth in FAR 52.227-19 (Commercial Computer
;;;    Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii) 
;;;    (Rights in Technical Data and Computer Software), as applicable.
;;;
;;;.
;;;
;;;    Note:
;;;            This file is loaded automatically by AutoCAD every time 
;;;            a drawing is opened.  It establishes an autoloader and
;;;            other utility functions.
;;;
;;;    Globalization Note:   
;;;            We do not support autoloading applications by the native 
;;;            language command call (e.g. with the leading underscore
;;;            mechanism.)


;;;===== Raster Image Support for Clipboard Paste Special =====
;;
;; IMAGEFILE
;;
;; Allow the IMAGE command to accept an image file name without
;; presenting the file dialog, even if filedia is on.
;; Example: (imagefile "c:/images/house.bmp")
;;
(defun imagefile (filename / filedia-save cmdecho-save)
  (setq filedia-save (getvar "FILEDIA"))
  (setq cmdecho-save (getvar "CMDECHO"))
  (setvar "FILEDIA" 0)
  (setvar "CMDECHO" 0)
  (command "_.-image" "_attach" filename)
  (setvar "FILEDIA" filedia-save)
  (setvar "CMDECHO" cmdecho-save)
  (princ)
)

;;;=== General Utility Functions ===

;   R12 compatibility - In R12 (acad_helpdlg) was an externally-defined 
;   ADS function.  Now it's a simple AutoLISP function that calls the 
;   built-in function (help).  It's only purpose is R12 compatibility.  
;   If you are calling it for anything else, you should almost certainly 
;   be calling (help) instead. 
 
(defun acad_helpdlg (helpfile topic)
  (help helpfile topic)
)


(defun *merr* (msg)
  (setq *error* m:err m:err nil)
  (princ)
)

(defun *merrmsg* (msg)
  (princ msg)
  (setq *error* m:err m:err nil)
  (princ)
)

;; Loads the indicated ARX app if it isn't already loaded
;; returns nil if no load was necessary, else returns the
;; app name if a load occurred.
(defun verify_arxapp_loaded (app) 
  (if (not (loadedp app (arx)))
      (arxload app f)
  )
)

;; determines if a given application is loaded...
;; general purpose: can ostensibly be used for appsets (arx) or (ads) or....
;;
;; app is the filename of the application to check (extension is required)
;; appset is a list of applications, (such as (arx) or (ads)
;; 
;; returns T or nil, depending on whether app is present in the appset
;; indicated.  Case is ignored in comparison, so "foo.arx" matches "FOO.ARX"
;; Also, if appset contains members that contain paths, app will right-match
;; against these members, so "bar.arx" matches "c:\\path\\bar.arx"; note that
;; "bar.arx" will *not* match "c:\\path\\foobar.arx."
(defun loadedp (app appset)
  (cond (appset  (or 
                     ;; exactly equal? (ignoring case)
                     (= (strcase (car appset))
                        (strcase app))
                     ;; right-matching? (ignoring case, but assuming that
                     ;; it's a complete filename (with a backslash before it)
					 (and 
					     (> (strlen (car appset)) (strlen app))
	                     (= (strcase (substr (car appset) 
	                                         (- (strlen (car appset)) 
	                                            (strlen app) 
	                                         ) 
	                                 )
	                        ) 
	                        (strcase (strcat "\\" app))
	                     )
				     )
                     ;; no match for this entry in appset, try next one....
                     (loadedp app (cdr appset)) )))
)


;;; ===== Single-line MText editor =====
(defun LispEd (contents / fname dcl state)
  (if (not (setq fname (getvar "program")))
     (setq fname "acad")
  )
  (strcat fname ".dcl")
  (setq dcl (load_dialog fname))
  (if (not (new_dialog "LispEd" dcl)) (exit))
  (set_tile "contents" contents)
  (mode_tile "contents" 2)
  (action_tile "contents" "(setq contents $value)")
  (action_tile "accept" "(done_dialog 1)")
  (action_tile "mtexted" "(done_dialog 2)" )
  (setq state (start_dialog))
  (unload_dialog dcl)
  (cond
    ((= state 1) contents)
    ((= state 2) -1)
    (t 0)
  )
)

;;; ===== Discontinued commands =====
(defun c:gifin ()
  (alert "\nGIFIN 명령은 더 이상 지원되지 않음.\nIMAGE 명령을 사용하여 래스터 이미지 파일을 부착하십시오.\n")
  (princ)
)

(defun c:pcxin ()
  (alert "\nPCXIN 명령은 더 이상 지원되지 않음.\nIMAGE 명령을 사용하여 래스터 이미지 파일을 부착하십시오.\n")
  (princ)
)

(defun c:tiffin ()
  (alert "\nTIFFIN 명령은 더 이상 지원되지 않음.\nIMAGE 명령을 사용하여 래스터 이미지 파일을 부착하십시오.\n")
  (princ)
)

(defun c:ddemodes()
  (alert "객체 특성 도구막대에 DDEMODES 기능이 통합됨.  \nDDEMODES는 계속되지 않음.  \n\n자세한 정보는, AutoCAD 도움말 색인 탭에서 DDEMODES를 선택하십시오.")
  (princ)
)

;;; ===== AutoLoad =====

;;; Check list of loaded <apptype> applications ("ads" or "arx")
;;; for the name of a certain appplication <appname>.
;;; Returns T if <appname> is loaded.

(defun ai_AppLoaded (appname apptype)
   (apply 'or
      (mapcar 
        '(lambda (j)
	    (wcmatch
               (strcase j T)
               (strcase (strcat "*" appname "*") T)
            )   
         )
	 (eval (list (read apptype)))
      )
   )
)

;;  
;;  Native Rx commands cannot be called with the "C:" syntax.  They must 
;;  be called via (command).  Therefore they require their own autoload 
;;  command.

(defun autonativeload (app cmdliste / qapp)
  (setq qapp (strcat "\"" app "\""))
  (setq initstring "\n초기화 중...")
  (mapcar
   '(lambda (cmd / nom_cmd native_cmd)
      (progn
        (setq nom_cmd (strcat "C:" cmd))
        (setq native_cmd (strcat "\"_" cmd "\""))
        (if (not (eval (read nom_cmd)))
            (eval
             (read (strcat
                    "(defun " nom_cmd "()"
                    "(setq m:err *error* *error* *merrmsg*)"
                    "(if (ai_ffile " qapp ")"
                    "(progn (princ initstring)"
                    "(_autoarxload " qapp ") (command " native_cmd "))"
                    "(ai_nofile " qapp "))"
                    "(setq *error* m:err m:err nil))"
                    ))))))
   cmdliste)
  nil
)

(defun _autoqload (quoi app cmdliste / qapp symnam)
  (setq qapp (strcat "\"" app "\""))
  (setq initstring "\n초기화 중...")
  (mapcar
   '(lambda (cmd / nom_cmd)
      (progn
        (setq nom_cmd (strcat "C:" cmd))
        (if (not (eval (read nom_cmd)))
            (eval
             (read (strcat
                    "(defun " nom_cmd "( / rtn)"
                    "(setq m:err *error* *error* *merrmsg*)"
                    "(if (ai_ffile " qapp ")"
                    "(progn (princ initstring)"
                    "(_auto" quoi "load " qapp ") (setq rtn (" nom_cmd ")))"
                    "(ai_nofile " qapp "))"
                    "(setq *error* m:err m:err nil)"
                    "rtn)"
                    ))))))
   cmdliste)
  nil
)

(defun autoload (app cmdliste)
  (_autoqload "" app cmdliste)
)

(defun autoxload (app cmdliste)
  (_autoqload "x" app cmdliste)
)

(defun autoarxload (app cmdliste)
  (_autoqload "arx" app cmdliste)
)

(defun autoarxacedload (app cmdliste / qapp symnam)
  (setq qapp (strcat "\"" app "\""))
  (setq initstring "\n초기화 중...")
  (mapcar
   '(lambda (cmd / nom_cmd)
      (progn
        (setq nom_cmd (strcat "C:" cmd))
        (if (not (eval (read nom_cmd)))
            (eval
             (read (strcat
                    "(defun " nom_cmd "( / oldcmdecho)"
                    "(setq m:err *error* *error* *merrmsg*)"
                    "(if (ai_ffile " qapp ")"
                    "(progn (princ initstring)"
                    "(_autoarxload " qapp ")"
                    "(setq oldcmdecho (getvar \"CMDECHO\"))"
                    "(setvar \"CMDECHO\" 0)"
                    "(command " "\"_" cmd "\"" ")"
                    "(setvar \"CMDECHO\" oldcmdecho))"
                    "(ai_nofile " qapp "))"
                    "(setq *error* m:err m:err nil)"
                    "(princ))"
                    ))))))
   cmdliste)
  nil
)

(defun _autoload (app)
; (princ "Auto:(load ") (princ app) (princ ")") (terpri)
  (load app)
)

(defun _autoxload (app)
; (princ "Auto:(xload ") (princ app) (princ ")") (terpri)
  (if (= app "region") (ai_select))
  (xload app)
  (if (= app "region") (ai_amegrey "~"))
)

(defun _autoarxload (app)
; (princ "Auto:(arxload ") (princ app) (princ ")") (terpri)
  (arxload app)
)

(defun ai_ffile (app)
  (or (findfile (strcat app ".lsp"))
      (findfile (strcat app ".exp"))
      (findfile (strcat app ".exe"))
      (findfile (strcat app ".arx"))
      (findfile app)
  )
)

(defun ai_nofile (filename)
  (princ
    (strcat "\n파일 "
            filename
            "(.lsp/.exe/.arx)을 검색 폴더에서 찾지 못함."
    )
  )
  (princ "\n지원 파일이 설치되었는지 점검하고 다시 시도하십시오.")
  (princ)
)


;;;===== AutoLoad LISP Applications =====
;  Set help for those apps with a command line interface

(autoload "appload" '("appload" "appload"))

(autoload "edge"  '("edge"))
(setfunhelp "C:edge" "" "edge")

(autoload "filter" '("filter " "filter"))

(autoload "3d" '("3d" "3d" "ai_box" "ai_pyramid" "ai_wedge" "ai_dome"
                 "ai_mesh" "ai_sphere" "ai_cone" "ai_torus" "ai_dish")
)
(setfunhelp "C:3d" "" "3d")
(setfunhelp "C:ai_box" "" "3d_box")
(setfunhelp "C:ai_pyramid" "" "3d_pyramid")
(setfunhelp "C:ai__wedge" "" "3d_wedge")
(setfunhelp "C:ai_dome" "" "3d_dome")
(setfunhelp "C:ai_mesh" "" "3d_mesh")
(setfunhelp "C:ai_sphere" "" "3d_sphere")
(setfunhelp "C:ai_cone" "" "3d_cone")
(setfunhelp "C:ai_torus" "" "3d_torus")
(setfunhelp "C:ai_dish" "" "3d_dish")

(autoload "ddinsert" '("ddinsert"))

(autoload "ddattdef" '("ddattdef"))

(autoload "ddattext" '("ddattext"))

(autoload "3darray" '("3darray"))
(setfunhelp "C:3darray" "" "3darray")

(autoload "ddmodify" '("ddmodify"))

(autoload "ddchprop" '("ddchprop"))

(autoload "ddview" '("ddview"))

(autoload "ddvpoint" '("ddvpoint"))

(autoload "mvsetup" '("mvsetup"))
(setfunhelp "C:mvsetup" "" "mvsetup")

(autoload "ddosnap" '("ddosnap"))

(autoload "ddptype" '("ddptype"))

(autoload "dducsp" '("dducsp"))

(autoload "ddunits" '("ddunits"))

(autoload "ddgrips" '("ddgrips"))

(autoload "ddselect" '("ddselect"))

(autoload "ddrename" '("ddrename"))

(autoload "ddcolor" '("ddcolor"))

(autoload "bmake" '("bmake"))

(autoload "attredef" '("attredef"))
(setfunhelp "C:attredef" "" "attredef")

(autoload "xplode" '("xp" "xplode"))
(setfunhelp "C:xplode" "" "xplode")

(autoload "tutorial" '("tutdemo" "tutclear"
				       "tutdemo" 
				       "tutclear"))

;; CalComp Configuration Command
(autoload "plpccw" '("cconfig"))


;;;===== AutoXLoad ADS Applications =====

(autoxload "hpmplot" ' ("hpconfig" "hpconfig" ))


;;;=========AutoArxLoad OCE Driver ARX applications ===========

(autoarxload "oceconf" '("oceconfig" "oceconfig"))

;;;===== AutoArxLoad Arx Applications =====

(autoarxload "geomcal" '("cal" "cal"))

(autoarxload "geom3d" '("mirror3d" "rotate3d" "align"
		      "mirror3d" "rotate3d" 
                                 "align"))


;;; ===== Double byte character handling functions =====

(defun is_lead_byte(code)
    (setq asia_cd (getvar "dwgcodepage"))
    (cond
        ( (or (= asia_cd "dos932")
              (= asia_cd "ANSI_932")
          )
          (or (and (<= 129 code) (<= code 159))
              (and (<= 224 code) (<= code 252))
          )
        )
        ( (or (= asia_cd "big5")
              (= asia_cd "ANSI_950")
          )
          (and (<= 161 code) (<= code 254))
        )
        ( (or (= asia_cd "johab")
              (= asia_cd "ANSI_1361")
          )
          (and (<= 132 code) (<= code 211))
        )
        ( (or (= asia_cd "ksc5601")
              (= asia_cd "ANSI_949")
          )
          (and (<= 161 code) (<= code 253))
        )
    )
)

;;; ====================================================


;;;
;;;  FITSTR2LEN
;;;
;;;  Truncates the given string to the given length. 
;;;  This function should be used to fit symbol table names, that
;;;  may turn into \U+ sequences into a given size to be displayed
;;;  inside a dialog box.
;;;
;;;  Ex: the following string: 
;;;
;;;      "This is a long string that will not fit into a 32 character static text box."
;;;
;;;      would display as a 32 character long string as follows:
;;;
;;;      "This is a long...tatic text box."
;;;

(defun fitstr2len (str1 maxlen)

    ;;; initialize internals
    (setq tmpstr str1)
    (setq len (strlen tmpstr))

    (if (> len maxlen) 
         (progn
            (setq maxlen2 (/ maxlen 2))
            (if (> maxlen (* maxlen2 2))
                 (setq maxlen2 (- maxlen2 1))
            )
            (if (is_lead_byte (substr tmpstr (- maxlen2 2) 1))
                 (setq tmpstr1 (substr tmpstr 1 (- maxlen2 3)))
                 (setq tmpstr1 (substr tmpstr 1 (- maxlen2 2)))
            )
            (if (is_lead_byte (substr tmpstr (- len (- maxlen2 1)) 1))
                 (setq tmpstr2 (substr tmpstr (- len (- maxlen2 3))))
                 (setq tmpstr2 (substr tmpstr (- len (- maxlen2 2))))
            )
            (setq str2 (strcat tmpstr1 "..." tmpstr2))
         ) ;;; progn
         (setq str2 (strcat tmpstr))
    ) ;;; if
) ;;; defun


;;;
;;;  If the first object in a selection set has an attached URL
;;;  Then launch browser and point to the URL.
;;;  Called by the Grips Cursor Menu
;;;

(defun C:gotourl ( / ssurl url i)
   (setq m:err *error* *error* *merrmsg* i 0)

; if some objects are not already pickfirst selected, 
; then allow objects to be selected

  (if (not (setq ssurl (ssget "I")))
      (setq ssurl (ssget))
  )

; if geturl LISP command not found then load arx application

  (if (/= (type geturl) 'EXRXSUBR)
    (arxload "dwfout")
  )
  
;  Search list for first object with an URL
  (while (and (= url nil) (< i (sslength ssurl)))
    (setq url (geturl (ssname ssurl i))
	  i (1+ i))
  )

; If an URL has be found, open browser and point to URL
  (if (= url nil)
    (alert "객체에 연관된 URL이 없음.")
    (command "_.browser" url)
  )

  (setq *error* m:err m:err nil)
  (princ)

)

;; Used by the import dialog to silently load a 3ds file
(defun import3ds (filename / filedia_old render)
  ;; Load Render if not loaded
  (setq render (findfile "render.arx"))
  (if render
    (verify_arxapp_loaded render) 
    (quit)
  )

  ;; Save current filedia & cmdecho setting.
  (setq filedia-save (getvar "FILEDIA"))
  (setq cmdecho-save (getvar "CMDECHO"))
  (setvar "FILEDIA" 0)
  (setvar "CMDECHO" 0)

  ;; Call 3DSIN and pass in filename.
  (c:3dsin 1 filename)

  ;; Reset filedia & cmdecho
  (setvar "FILEDIA" filedia-save)
  (setvar "CMDECHO" cmdecho-save)
  (princ)
)

;; Silent load.
(princ)

