
namespace bluetooth_jx


module HtmlBuilder = 

  let BuildHtml(msg, isLocal) =
      let align = 
        if isLocal then
            "align=\"left\" style=\"color:#0000FF\""
        else
            "align=\"right\" style=\"color:#FF0000\""
      "<div "+ align+">"+msg+"</div>"


