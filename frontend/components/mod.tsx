import { FC } from "react";

type Props = { mod: string | null }

export const Mod: FC<Props> = ( props ) => {
    if (props.mod == "RX")
    {
        return <span className="text-default-200">{props.mod}</span>;
    }
    else if (props.mod == "HD" || props.mod == "HR" || props.mod == "DT" || props.mod == "NC")
    {
        return <span className="text-default-500">{props.mod}</span>;
    }
    
    return (
        <span className="text-default-300">{props.mod}</span>
    );
  };
