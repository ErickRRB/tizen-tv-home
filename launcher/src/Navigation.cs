public static class Navigation {
 public static int Step(int count,int current,int direction)=>count<=0?-1:System.Math.Clamp(current+direction,0,count-1);
 public static int CircularStep(int count,int current,int direction)=>count<=0?-1:(current+direction%count+count)%count;
 public static int ShiftWindow(int count,int start,int target,int width,int direction){
  if(count<=width)return 0;start=(start%count+count)%count;
  for(int i=0;i<width;i++)if((start+i)%count==target)return start;
  return direction>0?(target-width+1+count)%count:target;
 }
}
