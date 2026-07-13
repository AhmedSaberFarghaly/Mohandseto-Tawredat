import { OrderDetailManager } from "./order-detail-manager";

export default async function OrderDetailsPage({params}:PageProps<"/dashboard/orders/[id]">){
  const {id}=await params; return <OrderDetailManager id={id}/>;
}
