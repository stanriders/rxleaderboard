import { FC } from "react";

type Props = { mod: string | null };

export const Mod: FC<Props> = (props) => {
  if (props.mod == "RX") {
    return <span className="text-default-200">{props.mod}</span>;
  }
  else if (props.mod == "EZ" || 
            props.mod == "FL" || 
            props.mod == "HD" || 
            props.mod == "HR" || 
            props.mod?.startsWith("DT") || 
            props.mod?.startsWith("NC") || 
            props.mod?.startsWith("HT") || 
            props.mod?.startsWith("DC")) {
    return <span className="text-default-500">{props.mod}</span>;
  }

  return (
    <span className="text-default-300">{props.mod}</span>
   );
};
