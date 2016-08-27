(defun join-list (l d) (
	(if [(len l) == 0]
		""
		[[(head l) + d] + (join-list (tail l) d)]
	)
))

(defun join (l d) (->
	[ret = ""]
	(for (range 0 (len l)) [(i) => (
		[ret += (+ [l : i] d)]
	)])
	(ret)
))

(defun assert (result msg...) (->
	(if result 
		(println "  [Passed] " (explode msg))
		(exit "Test failed !")
	)
))

(defun deep-compare-list (list-1 list-2) 
	"this function tests if two lists are equal"
	(->
	(if [[(type list-1) != "list"] or [(type list-2) != "list"]] 
		[list-1 == list-2] 
		(->
			[llen = (len list-1)]
			(if [llen != (len list-2)] (#f) (
				(if [llen == 0] (#t) (->
					[ret = #t]
					(for (range 0 llen) [(i) => 
						[ret = (deep-compare-list [list-1 : i] [list-2 : i])]
					])
					(ret) ; return ret
				)) ; end if
			)) ; end if
		)
	) ; end if
))

[map = [(f lst) => (->
	(for (range 0 (len lst)) [(i) => (
		[[lst : i] = (f [lst : i])]
	)])
	(lst)
)]]