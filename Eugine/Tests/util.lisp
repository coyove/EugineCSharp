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

(defun . (params...) [(.net-get-member [params : 0] [params : 1]) : 0])

(defun assert (result msg...) (->
	(if result 
		(println "  [Passed] " (explode msg))
		(-> 
			(var source [[~atom . @Token] . @Source])
			(var li [[~atom . @Token] . @LineIndex])
			(println "  [------] " (explode msg) " at " source ":" [li . @Item1] ":" [li . @Item2])
		)
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

[map := [(f lst) => (->
	(for (range 0 (len lst)) [(i) => (
		[[lst : i] = (f [lst : i])]
	)])
	(lst)
)]]

[filter = [(op lst) => (->
	[ret = (list)]
	(loop lst [(e) => (if (op e) [ret += e])])
	(ret) ; return
)]]

[random-list = [(n) => (->
	[nums = (range 0 n)]
	[ret = (list)]

	(loop [(len nums) > 0] [() => (->
		[idx = (floor [(random 0) * (len nums)])]
		[ret += [nums : idx]]
		(del nums idx)
	)])

	(ret)
)]]

;; never use this, it's experimental and fxxking slow, its existence only prove that ~parent is working
[pop := [(lst) => (if [(len lst) == 0] null
	(->
		[t := ""]
		(loop (keys ~parent) [(k) => (if [[~parent : k] == lst] (-> [t = k] #f))])
		(println t)

		[ret = (head lst)]
		[[~parent : t] = (tail lst)] ;; (del lst 0)
		ret
	)
)]]